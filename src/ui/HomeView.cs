using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ZenQOffice.Core;
using ZenQOffice.Format;
using System.Text;
using Graphics = ZenQOffice.UI.D2DGraphics;

namespace ZenQOffice.UI
{
    public class RecentDocItem
    {
        public string Title { get; set; }
        public string FilePath { get; set; }
        public OfficeAppMode Mode { get; set; }
        public string LastModified { get; set; }
        public bool IsStarred { get; set; }

        public RecentDocItem(string title, string path, OfficeAppMode mode, string date, bool isStarred = false)
        {
            Title = title;
            FilePath = path;
            Mode = mode;
            LastModified = date;
            IsStarred = isStarred;
        }
    }

    public class HomeView
    {
        public List<RecentDocItem> RecentDocs { get; set; }
        public List<RecentDocItem> TrashDocs { get; set; }
        public int ActiveTab { get; set; } // 0: Trang chủ, 1: Thùng rác, 2: Cài đặt, 3: Tài liệu của tôi
        public int SubFilter = 0; // 0: Tất cả, 1: Gần đây, 2: Đã gắn dấu sao, 10..14: Bộ lọc Tài liệu của tôi
        public float SmoothScrollY = 0f;
        public float TargetScrollY = 0f;
        public int HoverCardIndex = -1;
        public int HoverRowIndex = -1;
        public int HoverTabIndex = -1;
        public bool HoverHelpBtn = false;
        public bool HoverUpdateBtn = false;

        // Interaction Hover States for New UI Elements
        public bool HoverSidebarDoc = false;
        public bool HoverSidebarRecent = false;
        public bool HoverSidebarStar = false;
        public int HoverSidebarApp = -1;
        public bool HoverPromoBtn = false;
        public bool HoverStorageManage = false;
        public int HoverTemplateIndex = -1;
        public int HoverRowStar = -1;
        public bool HoverMyDocsOpenFileBtn = false;
        public int HoverPillIndex = -1;
        public bool HoverRightPanelUpdateBtn = false;
        public RectangleF RightPanelUpdateBtnRect = RectangleF.Empty;

        public struct FilterPill
        {
            public string Label;
            public int FilterId;
            public RectangleF Bounds;
        }
        public List<FilterPill> MyDocsFilterPills = new List<FilterPill>();
        public RectangleF MyDocsOpenFileBtnRect = RectangleF.Empty;

        public RectangleF ScrollBarBounds = RectangleF.Empty;
        public RectangleF ScrollBarThumbBounds = RectangleF.Empty;
        public bool IsDraggingScrollBar = false;
        public bool IsHoveringScrollBar = false;
        private int _scrollBarDragStartMouseY = 0;
        private float _scrollBarDragStartScrollY = 0;

        public bool IsHoverHand
        {
            get
            {
                return HoverTabIndex >= 0 || HoverCardIndex >= 0 || HoverRowIndex >= 0 ||
                       HoverHelpBtn || HoverUpdateBtn || HoverSidebarDoc || HoverSidebarRecent ||
                       HoverSidebarStar || HoverSidebarApp >= 0 || HoverPromoBtn ||
                       HoverStorageManage || HoverTemplateIndex >= 0 || HoverRowStar >= 0 ||
                       HoverMyDocsOpenFileBtn || HoverPillIndex >= 0 || HoverRightPanelUpdateBtn;
            }
        }

        // Settings Bounds (Tab 2: Cài đặt - Full Width Classic Style)
        public RectangleF SettingAiCopilotToggleRect = RectangleF.Empty;
        public RectangleF SettingAiProviderGoogleRect = RectangleF.Empty;
        public RectangleF SettingAiProviderOpenAiRect = RectangleF.Empty;
        public RectangleF SettingAiProviderClaudeRect = RectangleF.Empty;
        public RectangleF SettingAiProviderOllamaRect = RectangleF.Empty;
        public RectangleF SettingAiConfigBtnRect = RectangleF.Empty;
        public RectangleF SettingRibbonIslandRect = RectangleF.Empty;
        public RectangleF SettingRibbonStandardRect = RectangleF.Empty;
        public RectangleF SettingThemeLightRect = RectangleF.Empty;
        public RectangleF SettingThemeDarkRect = RectangleF.Empty;
        public RectangleF SettingAutoSaveToggleRect = RectangleF.Empty;
        public RectangleF SettingAutoSave30Rect = RectangleF.Empty;
        public RectangleF SettingAutoSave60Rect = RectangleF.Empty;
        public RectangleF SettingAutoSave120Rect = RectangleF.Empty;
        public RectangleF SettingSmoothScrollRect = RectangleF.Empty;
        public RectangleF SettingRulerRect = RectangleF.Empty;
        public RectangleF SettingAutoCorrectViRect = RectangleF.Empty;
        public RectangleF SettingWordPredictionRect = RectangleF.Empty;
        public RectangleF SettingLangViRect = RectangleF.Empty;
        public RectangleF SettingLangEnRect = RectangleF.Empty;
        public RectangleF SettingCheckUpdateBtnRect = RectangleF.Empty;
        public RectangleF SettingHelpBtnRect = RectangleF.Empty;
        public RectangleF SettingShortcutWriterRect = RectangleF.Empty;
        public RectangleF SettingShortcutCalcRect = RectangleF.Empty;
        public RectangleF SettingShortcutSlidesRect = RectangleF.Empty;
        public RectangleF SettingShortcutPdfRect = RectangleF.Empty;
        public RectangleF SettingAssocWriterRect = RectangleF.Empty;
        public RectangleF SettingAssocCalcRect = RectangleF.Empty;
        public RectangleF SettingAssocSlidesRect = RectangleF.Empty;
        public RectangleF SettingAssocPdfRect = RectangleF.Empty;
        public RectangleF SettingAssocSetAllBtnRect = RectangleF.Empty;
        public RectangleF SettingAssocUnsetAllBtnRect = RectangleF.Empty;
        public RectangleF SettingAssocWinSettingsRect = RectangleF.Empty;

        // Sliding Indicator Animation Properties
        public float IndicatorCurrentX = -1f;
        public float IndicatorTargetX = -1f;
        public float IndicatorCurrentW = -1f;
        public float IndicatorTargetW = -1f;

        public int ScrollY
        {
            get { return (int)Math.Round(SmoothScrollY); }
            set { SmoothScrollY = value; TargetScrollY = value; }
        }

        public bool UpdateSmoothScroll(int windowHeight = 0, int windowWidth = 0)
        {
            int h = (windowHeight > 200) ? windowHeight : _lastHeight;
            int w = (windowWidth > 200) ? windowWidth : _lastWidth;
            int maxScroll = GetMaxScroll(h, w);
            TargetScrollY = Math.Max(0, Math.Min(maxScroll, (float)Math.Round(TargetScrollY)));
            bool isAnimating = false;

            int prevInt = (int)Math.Round(SmoothScrollY);
            int targetInt = (int)TargetScrollY;

            if (prevInt == targetInt)
            {
                if (SmoothScrollY != TargetScrollY)
                {
                    SmoothScrollY = TargetScrollY;
                }
            }
            else
            {
                float diff = TargetScrollY - SmoothScrollY;
                float step = diff * 0.22f;
                float maxStep = 80f;
                if (Math.Abs(step) > maxStep) step = Math.Sign(step) * maxStep;

                float nextSmooth = SmoothScrollY + step;
                int nextInt = (int)Math.Round(nextSmooth);

                if (nextInt == prevInt)
                {
                    nextInt = prevInt + Math.Sign(diff);
                    nextSmooth = nextInt;
                }

                if ((diff > 0 && nextInt >= targetInt) || (diff < 0 && nextInt <= targetInt))
                {
                    SmoothScrollY = TargetScrollY;
                }
                else
                {
                    SmoothScrollY = nextSmooth;
                }
                isAnimating = true;
            }

            // Animate Sliding Indicator Underline
            if (IndicatorCurrentX > 0 && IndicatorTargetX > 0)
            {
                if (Math.Abs(IndicatorTargetX - IndicatorCurrentX) > 0.3f)
                {
                    IndicatorCurrentX += (IndicatorTargetX - IndicatorCurrentX) * 0.25f;
                    isAnimating = true;
                }
                else
                {
                    IndicatorCurrentX = IndicatorTargetX;
                }
            }

            if (IndicatorCurrentW > 0 && IndicatorTargetW > 0)
            {
                if (Math.Abs(IndicatorTargetW - IndicatorCurrentW) > 0.3f)
                {
                    IndicatorCurrentW += (IndicatorTargetW - IndicatorCurrentW) * 0.25f;
                    isAnimating = true;
                }
                else
                {
                    IndicatorCurrentW = IndicatorTargetW;
                }
            }

            return isAnimating;
        }

        public string SearchKeyword = "";
        public bool IsScanning = false;
        public bool IsSearchFocused = false;
        public bool CursorBlink = true;

        public List<RecentDocItem> GetFilteredDocs(int tab)
        {
            List<RecentDocItem> baseList = (tab == 1) ? TrashDocs : RecentDocs;
            if (baseList == null) return new List<RecentDocItem>();

            List<RecentDocItem> res = new List<RecentDocItem>();
            string kw = string.IsNullOrWhiteSpace(SearchKeyword) ? "" : SearchKeyword.Trim().ToLowerInvariant();

            foreach (var item in baseList)
            {
                if (tab == 0 && SubFilter == 2 && !item.IsStarred)
                {
                    continue;
                }
                if (tab == 3)
                {
                    if ((SubFilter == 2 || SubFilter == 14) && !item.IsStarred) continue;
                    if (SubFilter == 10 && item.Mode != OfficeAppMode.Writer) continue;
                    if (SubFilter == 11 && item.Mode != OfficeAppMode.Calc) continue;
                    if (SubFilter == 12 && item.Mode != OfficeAppMode.Slides) continue;
                    if (SubFilter == 13 && item.Mode != OfficeAppMode.Pdf) continue;
                    if (SubFilter == 15 && item.Mode != OfficeAppMode.Photo) continue;
                }

                if (!string.IsNullOrEmpty(kw))
                {
                    if ((item.Title != null && item.Title.ToLowerInvariant().Contains(kw)) ||
                        (item.FilePath != null && item.FilePath.ToLowerInvariant().Contains(kw)))
                    {
                        res.Add(item);
                    }
                }
                else
                {
                    res.Add(item);
                }
            }
            return res;
        }

        public int GetTotalContentHeight(int width, int windowHeight = 800)
        {
            if (ActiveTab == 2)
            {
                return 1450;
            }
            else
            {
                List<RecentDocItem> currentList = GetFilteredDocs(ActiveTab);
                int count = (currentList != null) ? currentList.Count : 0;
                return count * 58 + 384 + 96;
            }
        }

        public int GetMaxScroll(int windowHeight = 0, int width = 0)
        {
            int h = (windowHeight > 200) ? windowHeight : _lastHeight;
            int w = (width > 200) ? width : _lastWidth;
            if (ActiveTab == 2)
            {
                int totalH = 1500;
                int viewportH = Math.Max(100, h - 120);
                return Math.Max(0, totalH - viewportH);
            }
            else
            {
                List<RecentDocItem> currentList = GetFilteredDocs(ActiveTab);
                if (currentList == null || currentList.Count == 0) return 0;
                int totalListHeight = currentList.Count * 58 + 96;
                float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;
                GetCenterContentMetrics(w, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);
                int viewportH = Math.Max(100, h - (int)listTopY - 4);
                return Math.Max(0, totalListHeight - viewportH);
            }
        }

        public bool HandleScrollBarMouseDown(int mouseX, int mouseY, int width, int height)
        {
            if (ScrollBarBounds.IsEmpty) return false;
            if (ScrollBarBounds.Contains(mouseX, mouseY))
            {
                int maxScroll = GetMaxScroll(height, width);
                if (maxScroll <= 0) return true;

                if (ScrollBarThumbBounds.Contains(mouseX, mouseY))
                {
                    IsDraggingScrollBar = true;
                    _scrollBarDragStartMouseY = mouseY;
                    _scrollBarDragStartScrollY = TargetScrollY;
                    return true;
                }
                else
                {
                    float barH = ScrollBarBounds.Height;
                    float thumbH = Math.Max(20f, ScrollBarThumbBounds.Height);
                    float trackScrollH = Math.Max(1f, barH - thumbH);
                    float frac = (mouseY - ScrollBarBounds.Y - thumbH / 2f) / trackScrollH;
                    frac = Math.Max(0f, Math.Min(1f, frac));
                    TargetScrollY = (int)Math.Round(frac * maxScroll);
                    SmoothScrollY = TargetScrollY;

                    IsDraggingScrollBar = true;
                    _scrollBarDragStartMouseY = mouseY;
                    _scrollBarDragStartScrollY = TargetScrollY;
                    return true;
                }
            }
            return false;
        }

        public bool HandleScrollBarMouseMove(int mouseX, int mouseY, int width, int height)
        {
            if (IsDraggingScrollBar)
            {
                float barH = ScrollBarBounds.Height;
                float thumbH = Math.Max(20f, ScrollBarThumbBounds.Height);
                float trackScrollH = Math.Max(1f, barH - thumbH);
                int deltaMouseY = mouseY - _scrollBarDragStartMouseY;
                int maxScroll = GetMaxScroll(height, width);
                float deltaScroll = ((float)deltaMouseY / trackScrollH) * maxScroll;
                TargetScrollY = (int)Math.Max(0, Math.Min(maxScroll, _scrollBarDragStartScrollY + deltaScroll));
                SmoothScrollY = TargetScrollY;
                return true;
            }

            bool isInside = !ScrollBarBounds.IsEmpty && ScrollBarBounds.Contains(mouseX, mouseY);
            if (isInside != IsHoveringScrollBar)
            {
                IsHoveringScrollBar = isInside;
                return true;
            }
            return false;
        }

        public void HandleScrollBarMouseUp()
        {
            IsDraggingScrollBar = false;
        }

        public const float SidebarWidth = 220f;
        public const float RightPanelWidth = 280f;
        private int _lastWidth = 1200;
        private int _lastHeight = 800;

        public RectangleF GetSidebarBounds(int width, int height)
        {
            return new RectangleF(0, 0, SidebarWidth, height > 0 ? height : _lastHeight);
        }

        public RectangleF GetDockCapsuleBounds(int width, int scrollY)
        {
            return GetSidebarBounds(width, scrollY);
        }

        public RectangleF GetSearchBoxBounds(int width)
        {
            float contentLeft = SidebarWidth + 24f;
            float rightEdge = width - RightPanelWidth - 24f;
            float searchW = Math.Min(480f, rightEdge - contentLeft - 180f);
            if (searchW < 200f) searchW = 200f;
            return new RectangleF(contentLeft, 16f, searchW, 38f);
        }

        public RectangleF GetHelpBtnBounds(int width, int scrollY = 0)
        {
            float btnSize = 36f;
            return new RectangleF(width - RightPanelWidth - 24f - btnSize - 8f - btnSize, 17f, btnSize, btnSize);
        }

        public RectangleF GetUpdateBtnBounds(int width, int scrollY = 0)
        {
            float btnSize = 36f;
            return new RectangleF(width - RightPanelWidth - 24f - btnSize, 17f, btnSize, btnSize);
        }

        public RectangleF GetFeaturesTabBounds(int width, int scrollY = 0)
        {
            return GetCardBounds(0, width, _lastHeight);
        }

        public RectangleF GetMyDocsTabBounds(int width, int scrollY = 0)
        {
            return new RectangleF(12f, 70f, SidebarWidth - 24f, 38f);
        }

        public RectangleF GetOpenDocNavBounds(int width, int scrollY = 0)
        {
            return new RectangleF(12f, 114f, SidebarWidth - 24f, 38f);
        }

        public RectangleF GetRecentNavBounds(int width, int scrollY = 0)
        {
            return RectangleF.Empty;
        }

        public RectangleF GetStarredNavBounds(int width, int scrollY = 0)
        {
            return RectangleF.Empty;
        }

        public RectangleF GetTrashTabBounds(int width, int scrollY = 0)
        {
            return new RectangleF(12f, 158f, SidebarWidth - 24f, 38f);
        }

        public RectangleF GetAppNavBounds(int appIndex, int width, int scrollY = 0)
        {
            float appNavY = 236f + appIndex * 36f;
            return new RectangleF(12f, appNavY, SidebarWidth - 24f, 32f);
        }

        public RectangleF GetPromoBtnBounds(int width)
        {
            float panelX = width - RightPanelWidth;
            float px = panelX + 20f;
            return new RectangleF(px + 14f, 150f, 120f, 26f);
        }

        public RectangleF GetStorageManageBounds(int width)
        {
            float panelX = width - RightPanelWidth;
            float px = panelX + 20f;
            float pw = RightPanelWidth - 40f;
            return new RectangleF(px + pw - 48f, 204f, 48f, 20f);
        }

        public RectangleF GetTemplateCardBounds(int index, int width)
        {
            float panelX = width - RightPanelWidth;
            float px = panelX + 20f;
            float pw = RightPanelWidth - 40f;
            float curY = 294f + index * 44f;
            return new RectangleF(px, curY, pw, 40f);
        }

        public RectangleF GetSettingsTabBounds(int width, int scrollY = 0)
        {
            float h = (scrollY > 200) ? scrollY : _lastHeight;
            if (h < 200) h = 750f;
            return new RectangleF(12f, h - 52f, SidebarWidth - 24f, 38f);
        }

        public RectangleF GetAiTabBounds(int width, int scrollY = 0)
        {
            return GetSettingsTabBounds(width, scrollY);
        }

        public void GetCenterContentMetrics(int width, out float startX, out float contentW, out float bannerH, out float cardY, out float fileListY, out float colHeaderY, out float listTopY)
        {
            startX = SidebarWidth + 24f;
            contentW = Math.Max(400f, width - SidebarWidth - RightPanelWidth - 48f);
            bannerH = (ActiveTab == 0) ? (float)Math.Round(contentW * 3456.0 / 8064.0) : 0f;
            cardY = (ActiveTab == 0) ? (68f + bannerH + 16f) : 68f;
            if (ActiveTab == 0)
            {
                fileListY = cardY + 84f + 20f;
                colHeaderY = fileListY + 30f;
                listTopY = colHeaderY + 22f;
            }
            else if (ActiveTab == 3)
            {
                fileListY = 70f;
                colHeaderY = fileListY + 82f;
                listTopY = colHeaderY + 24f;
            }
            else // ActiveTab == 1 (Thùng rác)
            {
                fileListY = 70f;
                colHeaderY = fileListY + 36f;
                listTopY = colHeaderY + 22f;
            }
        }

        public RectangleF GetCardBounds(int index, int width, int windowHeight = 0)
        {
            if (index < 0 || index >= 5) return new RectangleF(-1000, -1000, 0, 0);
            float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;
            GetCenterContentMetrics(width, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);
            float cardGap = 12f;
            float cardW = Math.Min(148f, (contentW - 4 * cardGap) / 5f);
            if (cardW < 100f) cardW = 100f;
            return new RectangleF(startX + index * (cardW + cardGap), cardY, cardW, 84f);
        }

        public RectangleF GetTableCardBounds(int width, int windowHeight = 0)
        {
            float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;
            GetCenterContentMetrics(width, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);
            float h = (windowHeight > 200) ? windowHeight : _lastHeight;
            return new RectangleF(startX, listTopY, contentW, Math.Max(180, h - listTopY - 16f));
        }

        public HomeView()
        {
            ScrollY = 0;
            ActiveTab = 0;

            LoadRecentDocs();

            try
            {
                DesktopShortcutManager.DeleteShortcut("GenOffice");
            }
            catch { }
        }

        private static string GetRecentDocsFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "GenOffice");
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); } catch { }
            }
            return Path.Combine(dir, "recent_docs.xml");
        }

        private static readonly string[] DummyTitles = new string[]
        {
            "Kế hoạch Kinh doanh Quý 3.docx",
            "Báo cáo Tài chính Q2.xlsx",
            "Bài thuyết trình Sản phẩm 2026.pptx",
            "Hợp đồng Dịch vụ Phần mềm.pdf",
            "Dự thảo Bản nháp cũ.docx"
        };

        public void LoadRecentDocs()
        {
            RecentDocs = new List<RecentDocItem>();
            TrashDocs = new List<RecentDocItem>();

            string path = GetRecentDocsFilePath();
            if (File.Exists(path))
            {
                try
                {
                    System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                    doc.Load(path);

                    var rNodes = doc.SelectNodes("//RecentDocs/Doc");
                    if (rNodes != null)
                    {
                        foreach (System.Xml.XmlNode n in rNodes)
                        {
                            if (n.Attributes == null) continue;
                            string title = n.Attributes["title"] != null ? n.Attributes["title"].Value : "";
                            string fpath = n.Attributes["path"] != null ? n.Attributes["path"].Value : "";
                            int modeVal = 0;
                            if (n.Attributes["mode"] != null) int.TryParse(n.Attributes["mode"].Value, out modeVal);
                            string date = n.Attributes["date"] != null ? n.Attributes["date"].Value : "";
                            bool isStarred = false;
                            if (n.Attributes["starred"] != null) bool.TryParse(n.Attributes["starred"].Value, out isStarred);

                            // Loại bỏ các tệp mẫu ảo cũ không tồn tại
                            bool isDummy = false;
                            for (int d = 0; d < DummyTitles.Length; d++)
                            {
                                if (string.Equals(title, DummyTitles[d], StringComparison.OrdinalIgnoreCase) && !File.Exists(fpath))
                                {
                                    isDummy = true;
                                    break;
                                }
                            }
                            if (!isDummy)
                            {
                                RecentDocs.Add(new RecentDocItem(title, fpath, (OfficeAppMode)modeVal, date, isStarred));
                            }
                        }
                    }

                    var tNodes = doc.SelectNodes("//TrashDocs/Doc");
                    if (tNodes != null)
                    {
                        foreach (System.Xml.XmlNode n in tNodes)
                        {
                            if (n.Attributes == null) continue;
                            string title = n.Attributes["title"] != null ? n.Attributes["title"].Value : "";
                            string fpath = n.Attributes["path"] != null ? n.Attributes["path"].Value : "";
                            int modeVal = 0;
                            if (n.Attributes["mode"] != null) int.TryParse(n.Attributes["mode"].Value, out modeVal);
                            string date = n.Attributes["date"] != null ? n.Attributes["date"].Value : "";

                            bool isDummy = false;
                            for (int d = 0; d < DummyTitles.Length; d++)
                            {
                                if (string.Equals(title, DummyTitles[d], StringComparison.OrdinalIgnoreCase) && !File.Exists(fpath))
                                {
                                    isDummy = true;
                                    break;
                                }
                            }
                            if (!isDummy)
                            {
                                TrashDocs.Add(new RecentDocItem(title, fpath, (OfficeAppMode)modeVal, date));
                            }
                        }
                    }
                }
                catch { }
            }

            // Lưu lại phiên bản đã dọn sạch tệp ảo
            SaveRecentDocs();
        }

        public static void AddRecentDocStatic(string filePath, OfficeAppMode mode, string title = null)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            try
            {
                string xmlPath = GetRecentDocsFilePath();
                List<RecentDocItem> recent = new List<RecentDocItem>();
                List<RecentDocItem> trash = new List<RecentDocItem>();

                if (File.Exists(xmlPath))
                {
                    try
                    {
                        System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                        doc.Load(xmlPath);
                        var rNodes = doc.SelectNodes("//RecentDocs/Doc");
                        if (rNodes != null)
                        {
                            foreach (System.Xml.XmlNode n in rNodes)
                            {
                                if (n.Attributes == null) continue;
                                string t = n.Attributes["title"] != null ? n.Attributes["title"].Value : "";
                                string p = n.Attributes["path"] != null ? n.Attributes["path"].Value : "";
                                int m = 0;
                                if (n.Attributes["mode"] != null) int.TryParse(n.Attributes["mode"].Value, out m);
                                string d = n.Attributes["date"] != null ? n.Attributes["date"].Value : "";
                                recent.Add(new RecentDocItem(t, p, (OfficeAppMode)m, d));
                            }
                        }
                        var tNodes = doc.SelectNodes("//TrashDocs/Doc");
                        if (tNodes != null)
                        {
                            foreach (System.Xml.XmlNode n in tNodes)
                            {
                                if (n.Attributes == null) continue;
                                string t = n.Attributes["title"] != null ? n.Attributes["title"].Value : "";
                                string p = n.Attributes["path"] != null ? n.Attributes["path"].Value : "";
                                int m = 0;
                                if (n.Attributes["mode"] != null) int.TryParse(n.Attributes["mode"].Value, out m);
                                string d = n.Attributes["date"] != null ? n.Attributes["date"].Value : "";
                                trash.Add(new RecentDocItem(t, p, (OfficeAppMode)m, d));
                            }
                        }
                    }
                    catch { }
                }

                if (string.IsNullOrEmpty(title))
                {
                    title = Path.GetFileName(filePath);
                }

                // Xóa mục trùng lặp trước đó
                recent.RemoveAll(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase) || d.Title == title);
                trash.RemoveAll(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

                string dateStr = "Hôm nay, " + DateTime.Now.ToString("HH:mm");
                recent.Insert(0, new RecentDocItem(title, filePath, mode, dateStr));
                if (recent.Count > 40) recent.RemoveRange(40, recent.Count - 40);

                // Ghi lại vào file XML
                System.Xml.XmlDocument outDoc = new System.Xml.XmlDocument();
                System.Xml.XmlElement root = outDoc.CreateElement("GenOfficeDocs");
                outDoc.AppendChild(root);

                System.Xml.XmlElement rRoot = outDoc.CreateElement("RecentDocs");
                root.AppendChild(rRoot);
                foreach (var item in recent)
                {
                    System.Xml.XmlElement d = outDoc.CreateElement("Doc");
                    d.SetAttribute("title", item.Title ?? "");
                    d.SetAttribute("path", item.FilePath ?? "");
                    d.SetAttribute("mode", ((int)item.Mode).ToString());
                    d.SetAttribute("date", item.LastModified ?? "");
                    rRoot.AppendChild(d);
                }

                System.Xml.XmlElement tRoot = outDoc.CreateElement("TrashDocs");
                root.AppendChild(tRoot);
                foreach (var item in trash)
                {
                    System.Xml.XmlElement d = outDoc.CreateElement("Doc");
                    d.SetAttribute("title", item.Title ?? "");
                    d.SetAttribute("path", item.FilePath ?? "");
                    d.SetAttribute("mode", ((int)item.Mode).ToString());
                    d.SetAttribute("date", item.LastModified ?? "");
                    tRoot.AppendChild(d);
                }

                outDoc.Save(xmlPath);
            }
            catch { }
        }

        public void AddRecentDocument(string filePath, OfficeAppMode mode, string title = null)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            try
            {
                if (RecentDocs == null) RecentDocs = new List<RecentDocItem>();
                if (TrashDocs == null) TrashDocs = new List<RecentDocItem>();

                if (string.IsNullOrEmpty(title))
                {
                    title = Path.GetFileName(filePath);
                }

                RecentDocs.RemoveAll(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase) || d.Title == title);
                TrashDocs.RemoveAll(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

                string dateStr = "Hôm nay, " + DateTime.Now.ToString("HH:mm");
                RecentDocs.Insert(0, new RecentDocItem(title, filePath, mode, dateStr));

                if (RecentDocs.Count > 40)
                {
                    RecentDocs.RemoveRange(40, RecentDocs.Count - 40);
                }

                SaveRecentDocs();
            }
            catch { }
        }

        public void ScanDeviceForDocuments(Action onComplete = null)
        {
            if (IsScanning) return;
            IsScanning = true;

            System.Threading.ThreadPool.QueueUserWorkItem(state =>
            {
                int foundCount = 0;
                try
                {
                    List<string> targetFolders = new List<string>();
                    string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string downloads = Path.Combine(userProfile, "Downloads");

                    if (Directory.Exists(docs)) targetFolders.Add(docs);
                    if (Directory.Exists(desktop)) targetFolders.Add(desktop);
                    if (Directory.Exists(downloads)) targetFolders.Add(downloads);

                    string[] validExtensions = new string[] { ".docx", ".doc", ".odt", ".xlsx", ".xls", ".csv", ".pptx", ".ppt", ".pdf" };

                    List<FileInfo> allFiles = new List<FileInfo>();

                    foreach (var rootDir in targetFolders)
                    {
                        ScanDirectoryRecursive(rootDir, validExtensions, allFiles, 0, 3);
                    }

                    // Sắp xếp theo ngày sửa đổi mới nhất
                    allFiles.Sort((a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));

                    int maxToAdd = Math.Min(60, allFiles.Count);
                    for (int i = 0; i < maxToAdd; i++)
                    {
                        var fi = allFiles[i];
                        string ext = fi.Extension.ToLowerInvariant();
                        OfficeAppMode mode = OfficeAppMode.Writer;
                        if (ext == ".xlsx" || ext == ".xls" || ext == ".csv") mode = OfficeAppMode.Calc;
                        else if (ext == ".pptx" || ext == ".ppt") mode = OfficeAppMode.Slides;
                        else if (ext == ".pdf") mode = OfficeAppMode.Pdf;
                        else if (ext == ".svg" || ext == ".psd" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".tiff") mode = OfficeAppMode.Photo;

                        string dateStr = (fi.LastWriteTime.Date == DateTime.Today)
                            ? "Hôm nay, " + fi.LastWriteTime.ToString("HH:mm")
                            : fi.LastWriteTime.ToString("dd/MM/yyyy HH:mm");

                        if (RecentDocs == null) RecentDocs = new List<RecentDocItem>();
                        RecentDocs.RemoveAll(d => string.Equals(d.FilePath, fi.FullName, StringComparison.OrdinalIgnoreCase));
                        RecentDocs.Add(new RecentDocItem(fi.Name, fi.FullName, mode, dateStr));
                        foundCount++;
                    }

                    if (RecentDocs != null && RecentDocs.Count > 60)
                    {
                        RecentDocs.RemoveRange(60, RecentDocs.Count - 60);
                    }

                    SaveRecentDocs();
                }
                catch { }
                finally
                {
                    IsScanning = false;
                    if (onComplete != null) onComplete();
                }
            });
        }

        private void ScanDirectoryRecursive(string dir, string[] extensions, List<FileInfo> result, int currentDepth, int maxDepth)
        {
            if (currentDepth > maxDepth) return;
            try
            {
                DirectoryInfo di = new DirectoryInfo(dir);
                if ((di.Attributes & FileAttributes.Hidden) != 0 || (di.Attributes & FileAttributes.System) != 0) return;
                string dirName = di.Name.ToLowerInvariant();
                if (dirName.StartsWith(".") || dirName == "node_modules" || dirName == "appdata" || dirName == "temp" || dirName == "tmp" || dirName == "$recycle.bin") return;

                var files = di.GetFiles();
                foreach (var f in files)
                {
                    if ((f.Attributes & FileAttributes.Hidden) != 0) continue;
                    string ext = f.Extension.ToLowerInvariant();
                    for (int e = 0; e < extensions.Length; e++)
                    {
                        if (ext == extensions[e])
                        {
                            result.Add(f);
                            break;
                        }
                    }
                }

                if (currentDepth < maxDepth)
                {
                    var subDirs = di.GetDirectories();
                    foreach (var sd in subDirs)
                    {
                        ScanDirectoryRecursive(sd.FullName, extensions, result, currentDepth + 1, maxDepth);
                    }
                }
            }
            catch { }
        }

        public void EmptyTrash()
        {
            if (TrashDocs != null)
            {
                TrashDocs.Clear();
                SaveRecentDocs();
            }
        }

        public void ClearRecentDocs()
        {
            if (RecentDocs != null)
            {
                RecentDocs.Clear();
                SaveRecentDocs();
            }
        }

        public void SaveRecentDocs()
        {
            try
            {
                string path = GetRecentDocsFilePath();
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                System.Xml.XmlElement root = doc.CreateElement("GenOfficeDocs");
                doc.AppendChild(root);

                System.Xml.XmlElement rRoot = doc.CreateElement("RecentDocs");
                root.AppendChild(rRoot);
                if (RecentDocs != null)
                {
                    foreach (var item in RecentDocs)
                    {
                        System.Xml.XmlElement d = doc.CreateElement("Doc");
                        d.SetAttribute("title", item.Title ?? "");
                        d.SetAttribute("path", item.FilePath ?? "");
                        d.SetAttribute("mode", ((int)item.Mode).ToString());
                        d.SetAttribute("date", item.LastModified ?? "");
                        d.SetAttribute("starred", item.IsStarred ? "true" : "false");
                        rRoot.AppendChild(d);
                    }
                }

                System.Xml.XmlElement tRoot = doc.CreateElement("TrashDocs");
                root.AppendChild(tRoot);
                if (TrashDocs != null)
                {
                    foreach (var item in TrashDocs)
                    {
                        System.Xml.XmlElement d = doc.CreateElement("Doc");
                        d.SetAttribute("title", item.Title ?? "");
                        d.SetAttribute("path", item.FilePath ?? "");
                        d.SetAttribute("mode", ((int)item.Mode).ToString());
                        d.SetAttribute("date", item.LastModified ?? "");
                        tRoot.AppendChild(d);
                    }
                }

                doc.Save(path);
            }
            catch { }
        }

        #region Vector Graphics Drawing Primitives
        private GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = Math.Min(radius, Math.Min(rect.Width / 2f, rect.Height / 2f));
            if (r <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            float d = r * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void DrawVectorAppEmblem(Graphics g, float x, float y, float size, OfficeAppMode mode)
        {
            if (AppIconRenderer.DrawAppIcon(g, x, y, size, mode))
            {
                return;
            }

            RectangleF box = new RectangleF(x, y, size, size);
            Color baseCol, darkCol;

            if (mode == OfficeAppMode.Writer)
            {
                baseCol = Color.FromArgb(37, 99, 235);
                darkCol = Color.FromArgb(29, 78, 216);
            }
            else if (mode == OfficeAppMode.Calc)
            {
                baseCol = Color.FromArgb(16, 185, 129);
                darkCol = Color.FromArgb(5, 150, 105);
            }
            else if (mode == OfficeAppMode.Slides)
            {
                baseCol = Color.FromArgb(249, 115, 22);
                darkCol = Color.FromArgb(217, 70, 239);
            }
            else if (mode == OfficeAppMode.Photo)
            {
                baseCol = Color.FromArgb(99, 102, 241);
                darkCol = Color.FromArgb(79, 70, 229);
            }
            else // PDF
            {
                baseCol = Color.FromArgb(220, 38, 38);
                darkCol = Color.FromArgb(185, 28, 28);
            }

            using (GraphicsPath bPath = CreateRoundedRectanglePath(box, size * 0.24f))
            using (LinearGradientBrush bgBrush = new LinearGradientBrush(box, baseCol, darkCol, 45f))
            {
                g.FillPath(bgBrush, bPath);
            }

            float pad = size * 0.22f;
            RectangleF inner = new RectangleF(x + pad, y + pad, size - pad * 2, size - pad * 2);

            if (mode == OfficeAppMode.Writer)
            {
                using (GraphicsPath docP = CreateRoundedRectanglePath(inner, 3f))
                {
                    g.FillPath(Brushes.White, docP);
                }
                using (SolidBrush foldB = new SolidBrush(Color.FromArgb(219, 234, 254)))
                using (GraphicsPath foldP = new GraphicsPath())
                {
                    foldP.AddPolygon(new PointF[] {
                        new PointF(inner.Right - 8, inner.Top),
                        new PointF(inner.Right, inner.Top + 8),
                        new PointF(inner.Right - 8, inner.Top + 8)
                    });
                    g.FillPath(foldB, foldP);
                }
                using (Pen linePen = new Pen(Color.FromArgb(37, 99, 235), 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(linePen, inner.Left + 5, inner.Top + 9, inner.Right - 11, inner.Top + 9);
                    g.DrawLine(linePen, inner.Left + 5, inner.Top + 15, inner.Right - 5, inner.Top + 15);
                    g.DrawLine(linePen, inner.Left + 5, inner.Top + 21, inner.Right - 5, inner.Top + 21);
                    g.DrawLine(linePen, inner.Left + 5, inner.Top + 27, inner.Left + (inner.Width * 0.6f), inner.Top + 27);
                }
            }
            else if (mode == OfficeAppMode.Calc)
            {
                using (GraphicsPath sheetP = CreateRoundedRectanglePath(inner, 3f))
                {
                    g.FillPath(Brushes.White, sheetP);
                }
                using (SolidBrush thB = new SolidBrush(Color.FromArgb(16, 185, 129)))
                {
                    g.FillRectangle(thB, inner.Left, inner.Top, inner.Width, 8);
                }
                using (Pen gridPen = new Pen(Color.FromArgb(167, 243, 208), 1.2f))
                {
                    g.DrawLine(gridPen, inner.Left + inner.Width * 0.48f, inner.Top + 8, inner.Left + inner.Width * 0.48f, inner.Bottom);
                    g.DrawLine(gridPen, inner.Left, inner.Top + 15, inner.Right, inner.Top + 15);
                    g.DrawLine(gridPen, inner.Left, inner.Top + 22, inner.Right, inner.Top + 22);
                    g.DrawLine(gridPen, inner.Left, inner.Top + 29, inner.Right, inner.Top + 29);
                }
                using (SolidBrush barB = new SolidBrush(Color.FromArgb(5, 150, 105)))
                {
                    g.FillRectangle(barB, inner.Left + 4, inner.Bottom - 8, 3, 6);
                    g.FillRectangle(barB, inner.Left + 9, inner.Bottom - 12, 3, 10);
                    g.FillRectangle(barB, inner.Left + 14, inner.Bottom - 15, 3, 13);
                }
            }
            else if (mode == OfficeAppMode.Slides)
            {
                using (GraphicsPath slideP = CreateRoundedRectanglePath(inner, 3f))
                {
                    g.FillPath(Brushes.White, slideP);
                }
                RectangleF sInner = new RectangleF(inner.Left + 3, inner.Top + 3, inner.Width - 6, inner.Height - 6);
                using (LinearGradientBrush sBg = new LinearGradientBrush(sInner, Color.FromArgb(254, 243, 199), Color.FromArgb(254, 215, 170), 45f))
                {
                    g.FillRectangle(sBg, sInner);
                }
                using (SolidBrush pieB = new SolidBrush(Color.FromArgb(234, 88, 12)))
                using (SolidBrush barB = new SolidBrush(Color.FromArgb(249, 115, 22)))
                {
                    g.FillPie(pieB, sInner.Left + 3, sInner.Top + 4, 14, 14, 0, 270);
                    g.FillRectangle(barB, sInner.Left + 20, sInner.Top + 6, 8, 3);
                    g.FillRectangle(barB, sInner.Left + 20, sInner.Top + 11, 6, 3);
                }
            }
            else if (mode == OfficeAppMode.Pdf)
            {
                using (GraphicsPath pdfP = CreateRoundedRectanglePath(inner, 3f))
                {
                    g.FillPath(Brushes.White, pdfP);
                }
                using (SolidBrush foldB = new SolidBrush(Color.FromArgb(254, 202, 202)))
                using (GraphicsPath foldP = new GraphicsPath())
                {
                    foldP.AddPolygon(new PointF[] {
                        new PointF(inner.Right - 8, inner.Top),
                        new PointF(inner.Right, inner.Top + 8),
                        new PointF(inner.Right - 8, inner.Top + 8)
                    });
                    g.FillPath(foldB, foldP);
                }
                using (Font pdfFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                using (SolidBrush pdfTxt = new SolidBrush(Color.FromArgb(220, 38, 38)))
                {
                    g.DrawString("PDF", pdfFont, pdfTxt, inner.Left + 2, inner.Top + 8);
                }
            }
            else if (mode == OfficeAppMode.Photo)
            {
                using (GraphicsPath photoP = CreateRoundedRectanglePath(inner, 3f))
                {
                    g.FillPath(Brushes.White, photoP);
                }
                RectangleF pInner = new RectangleF(inner.Left + 2, inner.Top + 2, inner.Width - 4, inner.Height - 4);
                using (LinearGradientBrush pBg = new LinearGradientBrush(pInner, Color.FromArgb(238, 242, 255), Color.FromArgb(224, 231, 255), 45f))
                {
                    g.FillRectangle(pBg, pInner);
                }
                using (Font psFont = new Font("Segoe UI", Math.Max(7f, size * 0.32f), FontStyle.Bold))
                using (SolidBrush psTxt = new SolidBrush(Color.FromArgb(99, 102, 241)))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("Ps", psFont, psTxt, pInner, sf);
                }
            }
        }

        private void DrawVectorDocumentRowIcon(Graphics g, float x, float y, float w, float h, OfficeAppMode mode)
        {
            RectangleF rect = new RectangleF(x, y, w, h);
            Color col = (mode == OfficeAppMode.Writer) ? Color.FromArgb(37, 99, 235) :
                        ((mode == OfficeAppMode.Calc) ? Color.FromArgb(5, 150, 105) :
                        ((mode == OfficeAppMode.Slides) ? Color.FromArgb(234, 88, 12) :
                        ((mode == OfficeAppMode.Photo) ? Color.FromArgb(99, 102, 241) : Color.FromArgb(220, 38, 38))));

            using (GraphicsPath page = new GraphicsPath())
            using (SolidBrush pageBrush = new SolidBrush(Color.White))
            using (Pen borderPen = new Pen(col, 1.4f))
            {
                page.AddLine(x, y, x + w - 4, y);
                page.AddLine(x + w - 4, y, x + w, y + 4);
                page.AddLine(x + w, y + 4, x + w, y + h);
                page.AddLine(x + w, y + h, x, y + h);
                page.CloseFigure();

                g.FillPath(pageBrush, page);
                g.DrawPath(borderPen, page);

                using (SolidBrush foldB = new SolidBrush(col))
                using (GraphicsPath foldP = new GraphicsPath())
                {
                    foldP.AddPolygon(new PointF[] {
                        new PointF(x + w - 4, y),
                        new PointF(x + w, y + 4),
                        new PointF(x + w - 4, y + 4)
                    });
                    g.FillPath(foldB, foldP);
                }

                using (Pen linePen = new Pen(Color.FromArgb(140, col), 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(linePen, x + 3, y + 6, x + w - 5, y + 6);
                    g.DrawLine(linePen, x + 3, y + 9, x + w - 3, y + 9);
                    g.DrawLine(linePen, x + 3, y + 12, x + w * 0.7f, y + 12);
                }
            }
        }

        private void DrawSidebarNavItem(Graphics g, RectangleF rect, string text, Action<Graphics, float, float, float, Color> drawIcon, bool isActive, bool isHover)
        {
            if (isActive)
            {
                using (GraphicsPath ip = CreateRoundedRectanglePath(rect, 6f))
                using (SolidBrush ib = new SolidBrush(Color.White))
                using (Pen iPen = new Pen(Color.FromArgb(203, 213, 225), 1f))
                {
                    g.FillPath(ib, ip);
                    g.DrawPath(iPen, ip);
                }
                using (SolidBrush barB = new SolidBrush(Color.FromArgb(37, 99, 235)))
                {
                    g.FillRectangle(barB, rect.X, rect.Y + 7f, 3.5f, rect.Height - 14f);
                }
            }
            else if (isHover)
            {
                using (GraphicsPath hp = CreateRoundedRectanglePath(rect, 6f))
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(235, 238, 243)))
                {
                    g.FillPath(hb, hp);
                }
            }

            Color iconCol = isActive ? Color.FromArgb(37, 99, 235) : (isHover ? Color.FromArgb(15, 23, 42) : Color.FromArgb(100, 116, 139));
            if (drawIcon != null)
            {
                drawIcon(g, rect.X + 14f, rect.Y + (rect.Height - 18f) / 2f, 18f, iconCol);
            }

            using (Font f = new Font("Segoe UI", 9.8f, isActive ? FontStyle.Bold : FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(isActive ? Color.FromArgb(15, 23, 42) : (isHover ? Color.FromArgb(15, 23, 42) : Color.FromArgb(71, 85, 105))))
            {
                g.DrawString(text, f, tb, rect.X + 40f, rect.Y + (rect.Height - f.Height) / 2f);
            }
        }

        private void DrawTabItem(Graphics g, RectangleF tabRect, string text, Action<Graphics, float, float, float, Color> drawIcon, float iconSize, Color color, bool isActive, Font boldFont, Font regFont)
        {
            Font f = isActive ? boldFont : regFont;
            SizeF sz = g.MeasureString(text, f);
            float gap = 6f;
            float totalW = iconSize + gap + sz.Width;
            float startX = tabRect.X + (tabRect.Width - totalW) / 2f;
            float iconY = tabRect.Y + (tabRect.Height - iconSize) / 2f;
            float textY = tabRect.Y + (tabRect.Height - sz.Height) / 2f;

            if (drawIcon != null)
            {
                drawIcon(g, startX, iconY, iconSize, color);
            }
            using (SolidBrush tb = new SolidBrush(color))
            {
                g.DrawString(text, f, tb, startX + iconSize + gap, textY);
            }
        }

        private void DrawAiTabItem(Graphics g, RectangleF tabRect, string text, Action<Graphics, float, float, float, Color> drawIcon, float iconSize, Color color, bool isActive, Font boldFont, Font regFont)
        {
            Font f = isActive ? boldFont : regFont;
            string supText = "Thử nghiệm";

            using (Font supFont = new Font("Segoe UI", 7.0f, FontStyle.Bold))
            {
                SizeF mainSz = g.MeasureString(text, f);
                SizeF supSz = g.MeasureString(supText, supFont);

                float iconGap = 6f;
                float totalW = iconSize + iconGap + mainSz.Width - 1.5f + supSz.Width;

                float startX = tabRect.X + (tabRect.Width - totalW) / 2f;
                float iconY = tabRect.Y + (tabRect.Height - iconSize) / 2f;
                float textY = tabRect.Y + (tabRect.Height - mainSz.Height) / 2f;

                if (drawIcon != null)
                {
                    drawIcon(g, startX, iconY, iconSize, color);
                }

                float textX = startX + iconSize + iconGap;

                // 1. Chữ chính "Thiết lập AI" (căn giữa theo chiều dọc chuẩn của Tab)
                using (SolidBrush tb = new SolidBrush(color))
                {
                    g.DrawString(text, f, tb, textX, textY);
                }

                // 2. Chữ "Thử nghiệm" rõ nét, cân đối dạng số mũ ở góc trên bên phải (không nhô cao hơn đỉnh chữ)
                float supX = textX + mainSz.Width - 1.5f;
                float supY = textY + 0.5f;
                Color supCol = isActive ? Color.FromArgb(99, 102, 241) : Color.FromArgb(120, 135, 155);
                using (SolidBrush sb = new SolidBrush(supCol))
                {
                    g.DrawString(supText, supFont, sb, supX, supY);
                }
            }
        }

        public static void DrawFloatingCardBorderBadge(Graphics g, float x, float borderY)
        {
            float badgeW = 118f;
            float badgeH = 22f;
            float badgeY = borderY - badgeH / 2f;
            RectangleF badgeRect = new RectangleF(x, badgeY, badgeW, badgeH);

            // 1. Bóng đổ mềm mại (Soft drop shadow)
            RectangleF shadowRect = new RectangleF(badgeRect.X, badgeRect.Y + 1.5f, badgeRect.Width, badgeRect.Height);
            using (GraphicsPath sp = LiquidGlassHelper.CreateRoundedRectanglePath(shadowRect, 11f))
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(32, 15, 23, 42)))
            {
                g.FillPath(sb, sp);
            }

            // 2. Thân viên thuốc kính phát sáng Liquid Glass
            using (GraphicsPath bp = LiquidGlassHelper.CreateRoundedRectanglePath(badgeRect, 11f))
            using (LinearGradientBrush bg = new LinearGradientBrush(badgeRect, Color.FromArgb(255, 255, 255, 255), Color.FromArgb(235, 243, 255, 255), 90f))
            using (Pen bPen = new Pen(Color.FromArgb(165, 180, 252), 1.3f))
            {
                g.FillPath(bg, bp);
                g.DrawPath(bPen, bp);

                // Ánh gương bóng (Top gloss sheen)
                RectangleF glossR = new RectangleF(badgeRect.X + 2, badgeRect.Y + 1, badgeRect.Width - 4, (badgeH - 2) / 2f);
                using (GraphicsPath gp = LiquidGlassHelper.CreateRoundedRectanglePath(glossR, (badgeH - 2) / 4f))
                using (LinearGradientBrush gb = new LinearGradientBrush(glossR, Color.FromArgb(180, 255, 255, 255), Color.FromArgb(15, 255, 255, 255), 90f))
                {
                    g.FillPath(gb, gp);
                }
            }

            // 3. Chấm phát sáng Indigo & Chữ THỬ NGHIỆM
            float dotSize = 6f;
            float dotX = badgeRect.X + 10f;
            float dotY = badgeRect.Y + (badgeH - dotSize) / 2f;
            using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(99, 102, 241)))
            {
                g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
            }

            using (Font bf = new Font("Segoe UI", 7.6f, FontStyle.Bold))
            using (SolidBrush txtBrush = new SolidBrush(Color.FromArgb(67, 56, 202)))
            {
                string bText = "THỬ NGHIỆM";
                SizeF bSz = g.MeasureString(bText, bf);
                g.DrawString(bText, bf, txtBrush, dotX + dotSize + 6f, badgeRect.Y + (badgeH - bSz.Height) / 2f);
            }
        }

        private void DrawVectorSparkleIcon(Graphics g, float x, float y, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE781', new RectangleF(x, y, size, size), color, size * 0.72f);
        }

        private void DrawVectorFolderIcon(Graphics g, float x, float y, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE838', new RectangleF(x, y, size, size), color, size * 0.72f);
        }

        private void DrawVectorTrashIcon(Graphics g, float x, float y, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE74D', new RectangleF(x, y, size, size), color, size * 0.72f);
        }

        private void DrawVectorRestoreIcon(Graphics g, float x, float y, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE777', new RectangleF(x, y, size, size), color, size * 0.72f);
        }

        private void DrawVectorCrossIcon(Graphics g, float x, float y, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE711', new RectangleF(x, y, size, size), color, size * 0.72f);
        }

        private void DrawVectorSearchIcon(Graphics g, float cx, float cy, float size, Color col)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE721', new RectangleF(cx, cy, size, size), col, size * 0.72f);
        }

        private void DrawVectorArrowRight(Graphics g, float x, float y, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE76C', new RectangleF(x, y, size, size), color, size * 0.72f);
        }

        private void DrawVectorStarIcon(Graphics g, float cx, float cy, float radius, Color color, bool isFilled)
        {
            char glyph = isFilled ? '\uE735' : '\uE734';
            FluentIconRenderer.DrawGlyph(g, glyph, new RectangleF(cx - radius, cy - radius, radius * 2f, radius * 2f), color, radius * 1.5f);
        }

        private void DrawVectorQuestionIcon(Graphics g, float cx, float cy, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE897', new RectangleF(cx - size / 2f, cy - size / 2f, size, size), color, size * 0.72f);
        }

        private void DrawVectorGearIcon(Graphics g, float cx, float cy, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE713', new RectangleF(cx - size / 2f, cy - size / 2f, size, size), color, size * 0.72f);
        }

        private void ShowHelpDialog()
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "Giới thiệu & Hướng dẫn - ZENQ OFFICE";
                dlg.Size = new Size(620, 520);
                dlg.StartPosition = FormStartPosition.CenterScreen;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.White;
                dlg.Font = new Font("Segoe UI", 9.5f);

                Panel pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24) };
                dlg.Controls.Add(pnl);

                Label lblTitle = new Label
                {
                    Text = "ZENQ OFFICE 1",
                    Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 23, 42),
                    AutoSize = true,
                    Location = new Point(20, 20),
                    UseMnemonic = false
                };
                pnl.Controls.Add(lblTitle);

                Label lblSub = new Label
                {
                    Text = "Phát triển bởi NEXTGENEDUSPACE STUDIO • Tác giả: Trương Nguyễn Thế Danh © 2026",
                    Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(37, 99, 235),
                    AutoSize = true,
                    Location = new Point(20, 52),
                    UseMnemonic = false
                };
                pnl.Controls.Add(lblSub);

                Label lblDesc = new Label
                {
                    Text = "Bộ ứng dụng văn phòng đa năng hiện đại tích hợp đầy đủ:\n" +
                           "• Writer: Soạn thảo văn bản chuyên nghiệp (.docx)\n" +
                           "• Calc: Bảng tính số liệu thông minh (.xlsx, .csv)\n" +
                           "• Slides: Trình chiếu & Thuyết trình chuyên nghiệp (.pptx)\n" +
                           "• PDF: Đọc, chú thích, ký & chỉnh sửa tài liệu PDF\n\n" +
                           "Phím tắt nhanh:\n" +
                           "• Ctrl + N: Tạo tài liệu mới          • Ctrl + O: Mở tài liệu có sẵn\n" +
                           "• Ctrl + S: Lưu tài liệu hiện tại        • Ctrl + F: Tìm kiếm & Thay thế\n" +
                           "• F5: Bắt đầu trình chiếu Slide",
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = Color.FromArgb(51, 65, 85),
                    Location = new Point(20, 82),
                    Size = new Size(560, 185),
                    UseMnemonic = false
                };
                pnl.Controls.Add(lblDesc);

                // Khung thông báo lưu ý tương thích định dạng (Disclaimer Box)
                Panel pnlNote = new Panel
                {
                    Location = new Point(20, 275),
                    Size = new Size(560, 125),
                    BackColor = Color.FromArgb(248, 250, 252),
                    Padding = new Padding(12)
                };
                pnlNote.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                    using (Pen bPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                    using (GraphicsPath gp = CreateRoundedRectanglePath(new RectangleF(0.5f, 0.5f, pnlNote.Width - 1f, pnlNote.Height - 1f), 8f))
                    {
                        pe.Graphics.DrawPath(bPen, gp);
                    }
                    // Dải màu nhấn bên trái (Accent bar)
                    using (SolidBrush barB = new SolidBrush(Color.FromArgb(245, 158, 11)))
                    {
                        pe.Graphics.FillRectangle(barB, 0, 0, 4, pnlNote.Height);
                    }
                };

                Label lblNoteTitle = new Label
                {
                    Text = "• Lưu ý về tính tương thích định dạng:",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(180, 83, 9),
                    AutoSize = true,
                    Location = new Point(14, 10),
                    UseMnemonic = false
                };
                pnlNote.Controls.Add(lblNoteTitle);

                Label lblNoteContent = new Label
                {
                    Text = "ZENQ OFFICE luôn nỗ lực tối đa để hiển thị và xử lý tài liệu chuẩn xác nhất. Tuy nhiên, do sự khác biệt về đặc tả kỹ thuật và tiêu chuẩn phần mềm giữa các nền tảng, chúng tôi không thể cam kết rằng mọi định dạng nâng cao sẽ hiển thị giống hệt 100% như trên các bộ ứng dụng Office khác.",
                    Font = new Font("Segoe UI", 9f),
                    ForeColor = Color.FromArgb(71, 85, 105),
                    Location = new Point(14, 34),
                    Size = new Size(530, 80),
                    UseMnemonic = false
                };
                pnlNote.Controls.Add(lblNoteContent);

                pnl.Controls.Add(pnlNote);

                Button btnUpdate = new Button
                {
                    Text = "Kiểm tra cập nhật",
                    Image = FluentIconRenderer.GetGlyphBitmap('\uE72C', 14, 14, Color.FromArgb(15, 23, 42)),
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextImageRelation = TextImageRelation.ImageBeforeText,
                    Padding = new Padding(8, 0, 8, 0),
                    Size = new Size(160, 34),
                    Location = new Point(310, 418),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(241, 245, 249),
                    ForeColor = Color.FromArgb(15, 23, 42),
                    Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                    Cursor = Cursors.Hand
                };
                btnUpdate.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
                btnUpdate.Click += (s, e) =>
                {
                    dlg.Close();
                    using (UpdateDialogForm updDlg = new UpdateDialogForm(UpdateManager.LastReleaseInfo))
                    {
                        updDlg.ShowDialog();
                    }
                };
                pnl.Controls.Add(btnUpdate);

                Button btnClose = new Button
                {
                    Text = "Đóng",
                    DialogResult = DialogResult.OK,
                    Size = new Size(95, 34),
                    Location = new Point(485, 418),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnClose.FlatAppearance.BorderSize = 0;
                pnl.Controls.Add(btnClose);
                dlg.AcceptButton = btnClose;

                dlg.ShowDialog();
            }
        }
        #endregion

        #region Main Dashboard Drawing (OfficeHub Modern Style)
        
        // New helper methods needed for the redesign:
        private void DrawSidebarNavSmall(Graphics g, RectangleF rect, string text, Color textColor, bool isActive, bool isHover)
        {
            if (isActive)
            {
                using (GraphicsPath p = CreateRoundedRectanglePath(rect, 6f))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(239, 246, 255)))
                {
                    g.FillPath(b, p);
                }
                textColor = Color.FromArgb(37, 99, 235);
            }
            else if (isHover)
            {
                using (GraphicsPath p = CreateRoundedRectanglePath(rect, 6f))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    g.FillPath(b, p);
                }
                textColor = Color.FromArgb(15, 23, 42);
            }

            using (Font f = new Font("Segoe UI", 9f, isActive ? FontStyle.Bold : FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(textColor))
            {
                g.DrawString(text, f, tb, rect.X + 40f, rect.Y + (rect.Height - f.Height) / 2f);
            }
            // Small dot/dash icon
            using (SolidBrush dotB = new SolidBrush(isActive ? Color.FromArgb(37, 99, 235) : Color.FromArgb(160, textColor)))
            {
                float iconY = rect.Y + rect.Height / 2f - 2f;
                g.FillEllipse(dotB, rect.X + 22f, iconY, 4f, 4f);
            }
        }

        private void DrawSidebarAppNavItem(Graphics g, RectangleF rect, string text, OfficeAppMode mode, bool isHover)
        {
            if (isHover)
            {
                using (GraphicsPath p = CreateRoundedRectanglePath(rect, 6f))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    g.FillPath(b, p);
                }
            }

            // 3D App Icon
            float iconSize = 24f;
            float iconX = rect.X + 12f;
            float iconY = rect.Y + (rect.Height - iconSize) / 2f;
            if (!AppIconRenderer.DrawAppIcon(g, iconX, iconY, iconSize, mode))
            {
                DrawVectorAppEmblem(g, iconX, iconY, iconSize, mode);
            }

            // Text
            using (Font tf = new Font("Segoe UI", 9f, isHover ? FontStyle.Bold : FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(isHover ? Color.FromArgb(15, 23, 42) : Color.FromArgb(71, 85, 105)))
            {
                g.DrawString(text, tf, tb, rect.X + 44f, rect.Y + (rect.Height - tf.Height) / 2f);
            }
        }

        private void DrawVectorHomeIcon(Graphics g, float x, float y, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE80F', new RectangleF(x, y, size, size), color, size * 0.72f);
        }

        private void DrawVectorBellIcon(Graphics g, float x, float y, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uEA8F', new RectangleF(x, y, size, size), color, size * 0.72f);
        }

        private void DrawBannerFloatingIcon(Graphics g, float x, float y, float size, Color bgColor, string letter)
        {
            RectangleF iconRect = new RectangleF(x, y, size, size);
            using (GraphicsPath ip = CreateRoundedRectanglePath(iconRect, size * 0.25f))
            using (SolidBrush ib = new SolidBrush(Color.FromArgb(220, bgColor)))
            {
                g.FillPath(ib, ip);
            }
            using (Font lf = new Font("Segoe UI", size * 0.4f, FontStyle.Bold))
            using (SolidBrush lt = new SolidBrush(Color.White))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(letter, lf, lt, iconRect, sf);
            }
        }

        private void DrawCreationCard(Graphics g, RectangleF rect, string name, string subtitle, OfficeAppMode mode, bool isHover)
        {
            Color accentColor;
            if (mode == OfficeAppMode.Writer)
                accentColor = Color.FromArgb(37, 99, 235);
            else if (mode == OfficeAppMode.Calc)
                accentColor = Color.FromArgb(16, 185, 129);
            else if (mode == OfficeAppMode.Slides)
                accentColor = Color.FromArgb(249, 115, 22);
            else if (mode == OfficeAppMode.Photo)
                accentColor = Color.FromArgb(99, 102, 241);
            else
                accentColor = Color.FromArgb(220, 38, 38);

            // Card background
            using (GraphicsPath cp = CreateRoundedRectanglePath(rect, 12f))
            using (SolidBrush cb = new SolidBrush(Color.White))
            using (Pen cPen = new Pen(isHover ? accentColor : Color.FromArgb(234, 236, 240), isHover ? 1.5f : 1f))
            {
                if (isHover)
                {
                    RectangleF shadow = new RectangleF(rect.X, rect.Y + 2f, rect.Width, rect.Height);
                    using (GraphicsPath sp = CreateRoundedRectanglePath(shadow, 12f))
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                    {
                        g.FillPath(sb, sp);
                    }
                }
                g.FillPath(cb, cp);
                g.DrawPath(cPen, cp);
            }

            // Centered 3D icon
            float iconSize = 46f;
            float iconX = rect.X + (rect.Width - iconSize) / 2f;
            float iconY = isHover ? (rect.Y + 4f) : (rect.Y + 5f);
            if (!AppIconRenderer.DrawAppIcon(g, iconX, iconY, iconSize, mode))
            {
                DrawVectorAppEmblem(g, iconX, iconY, iconSize, mode);
            }

            // Name centered below icon
            using (Font nameFont = new Font("Segoe UI", 8.8f, FontStyle.Bold))
            using (SolidBrush nameB = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                StringFormat csf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                RectangleF nameRect = new RectangleF(rect.X + 2f, rect.Y + 51f, rect.Width - 4f, 17f);
                g.DrawString(name, nameFont, nameB, nameRect, csf);
            }

            // Subtitle centered
            using (Font subFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (SolidBrush subB = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                StringFormat csf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                RectangleF subRect = new RectangleF(rect.X + 2f, rect.Y + 67f, rect.Width - 4f, 14f);
                g.DrawString(subtitle, subFont, subB, subRect, csf);
            }
        }

        private struct TemplateLineInfo
        {
            public string Text;
            public int Alignment; // 0=Left, 1=Center, 2=Right, 3=Justify
            public bool Bold;
            public bool Italic;
            public float FontSize;

            public TemplateLineInfo(string text, int alignment, bool bold, bool italic, float fontSize)
            {
                Text = text;
                Alignment = alignment;
                Bold = bold;
                Italic = italic;
                FontSize = fontSize;
            }
        }

        private static void SaveFormattedTemplateDocx(string fPath, List<TemplateLineInfo> lines, string fontName = "Times New Roman")
        {
            StringBuilder sb = new StringBuilder();
            List<FormattedRun> runs = new List<FormattedRun>();
            Dictionary<int, int> paraAlignments = new Dictionary<int, int>();
            Dictionary<int, float> paraSpacingsAfter = new Dictionary<int, float>();
            Dictionary<int, float> paraLineSpacings = new Dictionary<int, float>();
            int charOffset = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                paraAlignments[charOffset] = line.Alignment;
                paraLineSpacings[charOffset] = 1.15f;
                if (line.Alignment == 1 && line.FontSize >= 14f)
                {
                    paraSpacingsAfter[charOffset] = 6f;
                }

                if (!string.IsNullOrEmpty(line.Text))
                {
                    runs.Add(new FormattedRun(
                        charOffset,
                        line.Text.Length,
                        fontName,
                        line.FontSize,
                        line.Bold,
                        line.Italic,
                        false,
                        false,
                        Color.FromArgb(15, 23, 42),
                        Color.Empty
                    ));
                    sb.Append(line.Text);
                }

                if (i < lines.Count - 1)
                {
                    sb.Append("\r\n");
                    charOffset += (line.Text != null ? line.Text.Length : 0) + 1;
                }
            }

            DocxHandler.SaveDocxFile(
                fPath,
                sb.ToString(),
                runs,
                false,
                0,
                "",
                "",
                false,
                null,
                null,
                paraAlignments,
                paraLineSpacings,
                null,
                paraSpacingsAfter
            );
        }

        public string EnsureTemplateLeaveRequest()
        {
            try
            {
                string tDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenOffice", "Templates");
                if (!Directory.Exists(tDir)) Directory.CreateDirectory(tDir);
                string fPath = Path.Combine(tDir, "Don_Xin_Nghi_Phep.docx");

                // Chuẩn hóa văn bản hành chính theo Nghị định 30/2020/NĐ-CP:
                // - Quốc hiệu, Tiêu ngữ, Tiêu đề: Căn giữa (Center), in đậm
                // - Nội dung: Giãn cách chuẩn, văn phong hành chính trang trọng
                // - Địa danh, ngày tháng & Chữ ký: Căn phải (Right) ở góc dưới cùng bên phải
                List<TemplateLineInfo> lines = new List<TemplateLineInfo>
                {
                    new TemplateLineInfo("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM", 1, true, false, 13f),
                    new TemplateLineInfo("Độc lập - Tự do - Hạnh phúc", 1, true, false, 14f),
                    new TemplateLineInfo("--------------------------------", 1, true, false, 12f),
                    new TemplateLineInfo("", 1, false, false, 13f),
                    new TemplateLineInfo("ĐƠN XIN NGHỈ PHÉP", 1, true, false, 16f),
                    new TemplateLineInfo("", 1, false, false, 13f),
                    new TemplateLineInfo("Kính gửi: Ban Giám đốc cùng Phòng Hành chính - Nhân sự", 0, false, false, 13f),
                    new TemplateLineInfo("", 0, false, false, 13f),
                    new TemplateLineInfo("Tôi tên là: [Họ và tên nhân viên]", 0, false, false, 13f),
                    new TemplateLineInfo("Chức vụ: [Chức danh / Vị trí công tác]", 0, false, false, 13f),
                    new TemplateLineInfo("Bộ phận / Phòng ban: [Phòng ban làm việc]", 0, false, false, 13f),
                    new TemplateLineInfo("Mã số nhân viên: [Mã NV]", 0, false, false, 13f),
                    new TemplateLineInfo("Số điện thoại liên hệ: [Số điện thoại]", 0, false, false, 13f),
                    new TemplateLineInfo("Email liên hệ: [Địa chỉ email cá nhân / công ty]", 0, false, false, 13f),
                    new TemplateLineInfo("", 0, false, false, 13f),
                    new TemplateLineInfo("Nay tôi làm đơn này kính xin Ban Giám đốc và Phòng Hành chính - Nhân sự cho phép tôi được nghỉ phép:", 0, false, false, 13f),
                    new TemplateLineInfo("- Thời gian nghỉ phép: Từ ngày .../.../2026 đến hết ngày .../.../2026", 0, false, false, 13f),
                    new TemplateLineInfo("- Tổng số ngày xin nghỉ: ... ngày làm việc (bằng chữ: ... ngày).", 0, false, false, 13f),
                    new TemplateLineInfo("- Lý do xin nghỉ phép: [Giải quyết việc cá nhân gia đình / Nghỉ phép năm theo quy định].", 0, false, false, 13f),
                    new TemplateLineInfo("- Địa chỉ và số điện thoại liên hệ khi cần: [Địa chỉ cư trú / Số điện thoại khẩn cấp].", 0, false, false, 13f),
                    new TemplateLineInfo("", 0, false, false, 13f),
                    new TemplateLineInfo("Trong thời gian nghỉ phép, tôi đã bàn giao đầy đủ công việc tồn đọng và hồ sơ tài liệu liên quan cho đồng nghiệp: [Họ và tên người nhận bàn giao] - Bộ phận: [Bộ phận]. Người nhận bàn giao cam kết sẽ hỗ trợ xử lý kịp thời các công việc phát sinh liên quan.", 3, false, false, 13f),
                    new TemplateLineInfo("Tôi cam kết sẽ có mặt làm việc đúng thời hạn quy định và hoàn thành đầy đủ nhiệm vụ được giao sau kỳ nghỉ.", 3, false, false, 13f),
                    new TemplateLineInfo("Kính mong Ban Giám đốc và Phòng Hành chính - Nhân sự xem xét và tạo điều kiện phê duyệt!", 3, false, false, 13f),
                    new TemplateLineInfo("Tôi xin chân thành cảm ơn!", 0, false, true, 13f),
                    new TemplateLineInfo("", 0, false, false, 13f),
                    new TemplateLineInfo("..., ngày ... tháng ... năm 2026", 2, false, true, 13f),
                    new TemplateLineInfo("Người làm đơn", 2, true, false, 13f),
                    new TemplateLineInfo("(Ký và ghi rõ họ tên)", 2, false, true, 12f),
                    new TemplateLineInfo("", 2, false, false, 13f),
                    new TemplateLineInfo("", 2, false, false, 13f),
                    new TemplateLineInfo("", 2, false, false, 13f),
                    new TemplateLineInfo("[Họ và tên nhân viên]", 2, true, false, 13f)
                };

                SaveFormattedTemplateDocx(fPath, lines);
                return fPath;
            }
            catch { return null; }
        }

        public string EnsureTemplateProjectReport()
        {
            try
            {
                string tDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenOffice", "Templates");
                if (!Directory.Exists(tDir)) Directory.CreateDirectory(tDir);
                string fPath = Path.Combine(tDir, "Bao_Cao_Tien_Do_Du_An.docx");

                List<TemplateLineInfo> lines = new List<TemplateLineInfo>
                {
                    new TemplateLineInfo("BÁO CÁO TIẾN ĐỘ DỰ ÁN", 1, true, false, 16f),
                    new TemplateLineInfo("Tháng 09/2026 • Ban Quản lý Dự án ZENQ OFFICE", 1, false, true, 12f),
                    new TemplateLineInfo("----------------------------------------------------------------", 1, false, false, 12f),
                    new TemplateLineInfo("", 0, false, false, 13f),
                    new TemplateLineInfo("1. THÔNG TIN CHUNG DỰ ÁN", 0, true, false, 13.5f),
                    new TemplateLineInfo("• Tên dự án: Triển khai Hệ thống Văn phòng Thông minh ZENQ OFFICE", 0, false, false, 13f),
                    new TemplateLineInfo("• Quản trị dự án (PM): [Họ và tên PM]", 0, false, false, 13f),
                    new TemplateLineInfo("• Giai đoạn: Thử nghiệm diện rộng (Pilot Phase)", 0, false, false, 13f),
                    new TemplateLineInfo("• Tình trạng hiện tại: ĐÚNG TIẾN ĐỘ (On Track)", 0, true, false, 13f),
                    new TemplateLineInfo("", 0, false, false, 13f),
                    new TemplateLineInfo("2. MỤC TIÊU & KẾT QUẢ ĐÃ ĐẠT ĐƯỢC", 0, true, false, 13.5f),
                    new TemplateLineInfo("- Hoàn tất tích hợp bộ công cụ Soạn thảo (Writer), Bảng tính (Calc), Trình chiếu (Slides) và PDF.", 3, false, false, 13f),
                    new TemplateLineInfo("- Tích hợp thành công Trợ lý AI Copilot hỗ trợ người dùng phân tích văn bản.", 3, false, false, 13f),
                    new TemplateLineInfo("- Tối ưu hóa hiệu năng render mượt mà 60 FPS trên máy tính cấu hình tiêu chuẩn.", 3, false, false, 13f),
                    new TemplateLineInfo("", 0, false, false, 13f),
                    new TemplateLineInfo("3. RỦI RO & GIẢI PHÁP ĐỀ XUẤT", 0, true, false, 13.5f),
                    new TemplateLineInfo("- Rủi ro về dung lượng bộ nhớ khi xử lý file lớn -> Đã áp dụng phân trang ảo (Virtual Rendering).", 3, false, false, 13f),
                    new TemplateLineInfo("- Đề xuất tăng cường thử nghiệm bảo mật cho các tệp văn bản mã hóa.", 3, false, false, 13f),
                    new TemplateLineInfo("", 0, false, false, 13f),
                    new TemplateLineInfo("4. KẾ HOẠCH HÀNH ĐỘNG TUẦN TIẾP THEO", 0, true, false, 13.5f),
                    new TemplateLineInfo("- Kiểm thử lần cuối và phát hành phiên bản chính thức đến người dùng.", 3, false, false, 13f),
                    new TemplateLineInfo("- Tổ chức buổi đào tạo hướng dẫn sử dụng các phím tắt và tính năng AI.", 3, false, false, 13f),
                    new TemplateLineInfo("", 0, false, false, 13f),
                    new TemplateLineInfo("..., ngày ... tháng ... năm 2026", 2, false, true, 13f),
                    new TemplateLineInfo("Người lập báo cáo", 2, true, false, 13f),
                    new TemplateLineInfo("(Ký và ghi rõ họ tên)", 2, false, true, 12f),
                    new TemplateLineInfo("", 2, false, false, 13f),
                    new TemplateLineInfo("", 2, false, false, 13f),
                    new TemplateLineInfo("", 2, false, false, 13f),
                    new TemplateLineInfo("[Họ và tên người lập]", 2, true, false, 13f)
                };

                SaveFormattedTemplateDocx(fPath, lines);
                return fPath;
            }
            catch { return null; }
        }

        public string EnsureTemplateBusinessPlan()
        {
            try
            {
                string tDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenOffice", "Templates");
                if (!Directory.Exists(tDir)) Directory.CreateDirectory(tDir);
                string fPath = Path.Combine(tDir, "Ke_Hoach_Kinh_Doanh.pptx");
                if (!File.Exists(fPath))
                {
                    PresentationRoot pres = new PresentationRoot();
                    pres.Title = "Kế hoạch Kinh doanh Chiến lược";

                    float cw = pres.CanvasWidth;
                    float ch = pres.CanvasHeight;

                    SlideNode s1 = new SlideNode { Layout = SlideLayoutType.TitleSlide, Title = "KẾ HOẠCH KINH DOANH CHIẾN LƯỢC" };
                    s1.Shapes.Add(new ShapeNode
                    {
                        Type = ShapeType.TextBox,
                        Name = "Tiêu đề Chính",
                        Transform = new Transform2D(cw * 0.1f, ch * 0.28f, cw * 0.8f, 100f),
                        Text = "KẾ HOẠCH KINH DOANH CHIẾN LƯỢC",
                        FontSize = 36f,
                        Bold = true,
                        Alignment = TextAlignment.Center,
                        VerticalAlignment = VerticalTextAlignment.Middle,
                        TextColor = 0x0F172A,
                        FillColor = 0x00FFFFFF,
                        StrokeWidth = 0f
                    });
                    s1.Shapes.Add(new ShapeNode
                    {
                        Type = ShapeType.TextBox,
                        Name = "Tiêu đề Phụ",
                        Transform = new Transform2D(cw * 0.15f, ch * 0.52f, cw * 0.7f, 60f),
                        Text = "Định hướng Phát triển Doanh nghiệp 2026 - 2030",
                        FontSize = 20f,
                        Alignment = TextAlignment.Center,
                        VerticalAlignment = VerticalTextAlignment.Middle,
                        TextColor = 0x64748B,
                        FillColor = 0x00FFFFFF,
                        StrokeWidth = 0f
                    });
                    pres.Slides.Add(s1);

                    SlideNode s2 = new SlideNode { Layout = SlideLayoutType.TitleAndContent, Title = "TỔNG QUAN THỊ TRƯỜNG & MỤC TIÊU" };
                    s2.Shapes.Add(new ShapeNode
                    {
                        Type = ShapeType.TextBox,
                        Name = "Tiêu đề",
                        Transform = new Transform2D(cw * 0.06f, ch * 0.08f, cw * 0.88f, 70f),
                        Text = "TỔNG QUAN THỊ TRƯỜNG & MỤC TIÊU",
                        FontSize = 30f,
                        Bold = true,
                        Alignment = TextAlignment.Left,
                        VerticalAlignment = VerticalTextAlignment.Middle,
                        TextColor = 0x0F172A,
                        FillColor = 0x00FFFFFF,
                        StrokeWidth = 0f
                    });
                    s2.Shapes.Add(new ShapeNode
                    {
                        Type = ShapeType.TextBox,
                        Name = "Nội dung",
                        Transform = new Transform2D(cw * 0.06f, ch * 0.26f, cw * 0.88f, ch * 0.62f),
                        Text = "• Nhu cầu chuyển đổi số trong văn phòng tăng trưởng 35%/năm.\n• Mục tiêu đạt 100.000 người dùng hoạt động hàng tháng (MAU).\n• Doanh thu kỳ vọng năm đầu tiên: 10 tỷ VNĐ.\n• Mở rộng hợp tác chiến lược cùng các đối tác công nghệ.",
                        FontSize = 18f,
                        Alignment = TextAlignment.Left,
                        TextColor = 0x334155,
                        FillColor = 0x00FFFFFF,
                        StrokeWidth = 0f
                    });
                    pres.Slides.Add(s2);

                    Format.PptxHandler.SavePptx(fPath, pres);
                }
                return fPath;
            }
            catch { return null; }
        }

        public string EnsureTemplateTimesheet()
        {
            try
            {
                string tDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenOffice", "Templates");
                if (!Directory.Exists(tDir)) Directory.CreateDirectory(tDir);
                string fPath = Path.Combine(tDir, "Bang_Cham_Cong_Thang.csv");
                if (!File.Exists(fPath))
                {
                    string csv = "STT,Mã NV,Họ và Tên,Chức vụ,Số ngày công,Nghỉ phép,Nghỉ ốm,Tổng công,Ghi chú\r\n1,NV001,Nguyễn Văn An,Trưởng phòng,22,0,0,22,Đủ công\r\n2,NV002,Trần Thị Bình,Chuyên viên,21,1,0,22,Phép năm\r\n3,NV003,Lê Hoàng Cường,Kỹ sư phần mềm,22,0,0,22,Đủ công\r\n4,NV004,Phạm Thị Dung,Kế toán trưởng,20,2,0,22,Nghỉ việc riêng\r\n5,NV005,Võ Quốc Em,Nhân viên thiết kế,22,0,0,22,Đủ công\r\n";
                    File.WriteAllText(fPath, csv, System.Text.Encoding.UTF8);
                }
                return fPath;
            }
            catch { return null; }
        }

        private void DrawRightSidebarPanel(Graphics g, int width, int height)
        {
            float panelX = width - RightPanelWidth;
            float panelW = RightPanelWidth;

            // Panel background
            using (SolidBrush panelBg = new SolidBrush(Color.White))
            using (Pen panelBorder = new Pen(Color.FromArgb(234, 236, 240), 1f))
            {
                g.FillRectangle(panelBg, panelX, 0, panelW, height);
                g.DrawLine(panelBorder, panelX, 0, panelX, height);
            }

            float px = panelX + 20f;
            float pw = panelW - 40f;

            // ── 1. "Làm việc hiệu quả hơn" promo card ──
            RectangleF promoRect = new RectangleF(px, 70f, pw, 118f);
            using (GraphicsPath pp = CreateRoundedRectanglePath(promoRect, 12f))
            using (LinearGradientBrush promoBg = new LinearGradientBrush(promoRect, Color.FromArgb(238, 242, 255), Color.FromArgb(224, 231, 255), 135f))
            {
                g.FillPath(promoBg, pp);
            }
            using (Font promoTitleFont = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (Font promoSubFont = new Font("Segoe UI", 8f, FontStyle.Regular))
            using (SolidBrush promoTitleB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (SolidBrush promoSubB = new SolidBrush(Color.FromArgb(71, 85, 105)))
            {
                g.DrawString("Làm việc hiệu quả hơn", promoTitleFont, promoTitleB, px + 14f, 82f);
                g.DrawString("Tận dụng các mẫu tài liệu có sẵn\nđể tiết kiệm thời gian và nâng cao\nhiệu suất công việc.", promoSubFont, promoSubB, px + 14f, 104f);
            }
            // CTA button
            RectangleF ctaBtn = GetPromoBtnBounds(width);
            using (GraphicsPath ctap = CreateRoundedRectanglePath(ctaBtn, 6f))
            using (SolidBrush ctaBg = new SolidBrush(HoverPromoBtn ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235)))
            {
                g.FillPath(ctaBg, ctap);
            }
            using (Font ctaFont = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (SolidBrush ctaTxt = new SolidBrush(Color.White))
            {
                StringFormat csf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("Khám phá mẫu", ctaFont, ctaTxt, ctaBtn, csf);
            }

            // ── 2. "Dung lượng lưu trữ" section (DỮ LIỆU THỰC TẾ TỪ Ổ ĐĨA MÁY TÍNH) ──
            string driveLetter = "C:";
            long totalBytes = 0;
            long freeBytes = 0;
            try
            {
                string sysPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string root = Path.GetPathRoot(sysPath);
                if (!string.IsNullOrEmpty(root))
                {
                    DriveInfo drive = new DriveInfo(root);
                    driveLetter = drive.Name.TrimEnd('\\');
                    totalBytes = drive.TotalSize;
                    freeBytes = drive.AvailableFreeSpace;
                }
            }
            catch
            {
                try
                {
                    DriveInfo d = new DriveInfo("C");
                    driveLetter = "C:";
                    totalBytes = d.TotalSize;
                    freeBytes = d.AvailableFreeSpace;
                }
                catch { }
            }

            long usedBytes = Math.Max(0, totalBytes - freeBytes);
            double usedGb = Math.Round(usedBytes / (1024.0 * 1024 * 1024), 1);
            double totalGb = (totalBytes > 0) ? Math.Round(totalBytes / (1024.0 * 1024 * 1024), 1) : 100.0;
            int usedPct = (totalBytes > 0) ? (int)Math.Max(1, Math.Min(99, Math.Round((double)usedBytes / (double)totalBytes * 100.0))) : 35;

            float storeY = 204f;
            using (Font storeTitleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font storeSubFont = new Font("Segoe UI", 8f, FontStyle.Regular))
            using (SolidBrush storeTitleB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString(string.Format("Dung lượng ổ đĩa ({0})", driveLetter), storeTitleFont, storeTitleB, px, storeY);
                using (SolidBrush linkB = new SolidBrush(HoverStorageManage ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235)))
                {
                    g.DrawString("Quản lý", storeSubFont, linkB, px + pw - 48f, storeY + 2f);
                }
            }

            // Progress bar
            RectangleF trackRect = new RectangleF(px, 226f, pw, 7f);
            using (GraphicsPath trp = CreateRoundedRectanglePath(trackRect, 3.5f))
            using (SolidBrush trBg = new SolidBrush(Color.FromArgb(226, 232, 240)))
            {
                g.FillPath(trBg, trp);
            }
            float fillW = Math.Max(7f, pw * (usedPct / 100f));
            RectangleF fillRect = new RectangleF(px, 226f, fillW, 7f);
            using (GraphicsPath flp = CreateRoundedRectanglePath(fillRect, 3.5f))
            using (SolidBrush flBg = new SolidBrush(usedPct > 90 ? Color.FromArgb(239, 68, 68) : Color.FromArgb(37, 99, 235)))
            {
                g.FillPath(flBg, flp);
            }

            using (Font storeInfoFont = new Font("Segoe UI", 8f, FontStyle.Regular))
            using (SolidBrush storeInfoB = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                g.DrawString(string.Format("{0} GB / {1} GB", usedGb, totalGb), storeInfoFont, storeInfoB, px, 238f);
                g.DrawString(string.Format("{0}%", usedPct), storeInfoFont, storeInfoB, px + pw - 28f, 238f);
            }

            // ── 3. "Mẫu nổi bật" section (CLICKABLE & FUNCTIONAL) ──
            float tmplHeaderY = 266f;
            using (Font secFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (SolidBrush secB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Mẫu nổi bật", secFont, secB, px, tmplHeaderY);
            }
            using (Font linkFont = new Font("Segoe UI", 8f, FontStyle.Regular))
            using (SolidBrush linkB = new SolidBrush(Color.FromArgb(37, 99, 235)))
            {
                g.DrawString("Xem tất cả", linkFont, linkB, px + pw - 50f, tmplHeaderY + 2f);
            }

            // Template cards (Starts at Y=294f, each height 40f with 44f step)
            string[] templateNames = new string[] { "Đơn xin nghỉ phép", "Kế hoạch kinh doanh", "Báo cáo dự án", "Bảng chấm công" };
            string[] templateTypes = new string[] { "Mẫu văn bản", "Mẫu thuyết trình", "Mẫu văn bản", "Mẫu bảng tính" };
            Color[] templateColors = new Color[] { Color.FromArgb(99, 102, 241), Color.FromArgb(249, 115, 22), Color.FromArgb(37, 99, 235), Color.FromArgb(16, 185, 129) };
            string[] templateLetters = new string[] { "W", "P", "W", "X" };
            OfficeAppMode[] templateModes = new OfficeAppMode[] { OfficeAppMode.Writer, OfficeAppMode.Slides, OfficeAppMode.Writer, OfficeAppMode.Calc };

            for (int i = 0; i < templateNames.Length; i++)
            {
                RectangleF tmplRect = GetTemplateCardBounds(i, width);
                bool isTmplHover = (HoverTemplateIndex == i);

                if (isTmplHover)
                {
                    using (GraphicsPath hovP = CreateRoundedRectanglePath(tmplRect, 6f))
                    using (SolidBrush hovB = new SolidBrush(Color.FromArgb(241, 245, 249)))
                    {
                        g.FillPath(hovB, hovP);
                    }
                }

                // Template icon
                RectangleF tmplIcon = new RectangleF(tmplRect.X + 4f, tmplRect.Y + 6f, 28f, 28f);
                if (!AppIconRenderer.DrawAppIcon(g, tmplIcon.X, tmplIcon.Y, tmplIcon.Width, templateModes[i]))
                {
                    using (GraphicsPath tip = CreateRoundedRectanglePath(tmplIcon, 6f))
                    using (SolidBrush tib = new SolidBrush(Color.FromArgb(30, templateColors[i].R, templateColors[i].G, templateColors[i].B)))
                    {
                        g.FillPath(tib, tip);
                    }
                    using (Font tlf = new Font("Segoe UI", 9f, FontStyle.Bold))
                    using (SolidBrush tlt = new SolidBrush(templateColors[i]))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(templateLetters[i], tlf, tlt, tmplIcon, sf);
                    }
                }
                // Template text
                using (Font tnf = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (Font ttf = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                using (SolidBrush tnb = new SolidBrush(Color.FromArgb(30, 41, 59)))
                using (SolidBrush ttb = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString(templateNames[i], tnf, tnb, tmplRect.X + 38f, tmplRect.Y + 6f);
                    g.DrawString(templateTypes[i], ttf, ttb, tmplRect.X + 38f, tmplRect.Y + 23f);
                }
                // Arrow >
                using (SolidBrush arB = new SolidBrush(isTmplHover ? Color.FromArgb(37, 99, 235) : Color.FromArgb(148, 163, 184)))
                using (Font arf = new Font("Segoe UI", 10f, FontStyle.Regular))
                {
                    g.DrawString("›", arf, arB, tmplRect.Right - 14f, tmplRect.Y + 10f);
                }

                // Separator line
                if (i < templateNames.Length - 1)
                {
                    using (Pen sepPen = new Pen(Color.FromArgb(241, 245, 249), 1f))
                    {
                        g.DrawLine(sepPen, tmplRect.X, tmplRect.Bottom + 1f, tmplRect.Right, tmplRect.Bottom + 1f);
                    }
                }
            }

            // ── 4. "Tính năng nổi bật" section (TÍNH NĂNG THẬT 100% CỦA GENOFFICE) ──
            float featY = 482f;
            using (Font secFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (SolidBrush secB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Tính năng nổi bật", secFont, secB, px, featY);
            }

            string[] features = new string[] {
                "Trợ lý AI Copilot thông minh",
                "Hỗ trợ DOCX, XLSX, PPTX, PDF",
                "Ngoại tuyến 100% & Bảo mật cao",
                "Tự động lưu bản thảo an toàn"
            };
            using (Font featFont = new Font("Segoe UI", 8.2f, FontStyle.Regular))
            using (SolidBrush featB = new SolidBrush(Color.FromArgb(71, 85, 105)))
            using (SolidBrush checkB = new SolidBrush(Color.FromArgb(16, 185, 129)))
            {
                for (int i = 0; i < features.Length; i++)
                {
                    float fy = featY + 22f + i * 22f;
                    FluentIconRenderer.DrawGlyph(g, '\uE73E', new RectangleF(px + 2f, fy + 2f, 12f, 12f), Color.FromArgb(16, 185, 129), 11f);
                    g.DrawString(features[i], featFont, featB, px + 18f, fy);
                }
            }

            // ── 5. Nút "Cập nhật ngay" nhỏ gọn, bo tròn chuẩn Win11, xích xuống góc dưới cùng ──
            // Nếu phiên bản hiện tại đã là mới nhất (không có bản cập nhật mới), ẩn hoàn toàn nút cập nhật
            bool hasNewUpdate = (UpdateManager.LastReleaseInfo != null && UpdateManager.LastReleaseInfo.HasUpdate);
            if (!hasNewUpdate)
            {
                RightPanelUpdateBtnRect = RectangleF.Empty;
                return;
            }

            float btnW = 126f;
            float btnH = 26f;
            float btnX = px + pw - btnW; // Xích sát góc phải
            float btnY = Math.Max(featY + 22f + features.Length * 22f + 20f, height - 38f); // Xích sát góc đáy

            RightPanelUpdateBtnRect = new RectangleF(btnX, btnY, btnW, btnH);

            Color btnBg = HoverRightPanelUpdateBtn ? Color.FromArgb(185, 28, 28) : Color.FromArgb(220, 38, 38);
            Color btnBorder = Color.FromArgb(185, 28, 28);
            Color btnText = Color.White;

            // Bo tròn dạng capsule / viên thuốc bo tròn 100%
            using (GraphicsPath btnPath = CreateRoundedRectanglePath(RightPanelUpdateBtnRect, btnH / 2f))
            using (SolidBrush bgB = new SolidBrush(btnBg))
            using (Pen borderP = new Pen(btnBorder, HoverRightPanelUpdateBtn ? 1.2f : 1f))
            {
                g.FillPath(bgB, btnPath);
                g.DrawPath(borderP, btnPath);
            }

            // Vẽ icon Win11 Fluent Update
            float iconSize = 14f;
            float iconX = btnX + 10f;
            float iconY = btnY + (btnH - iconSize) / 2f;
            DrawWin11UpdateIcon(g, iconX, iconY, iconSize, btnText);

            // Chữ "Cập nhật ngay"
            using (Font btnF = new Font("Segoe UI", 8.2f, FontStyle.Bold))
            using (SolidBrush textB = new SolidBrush(btnText))
            {
                g.DrawString("Cập nhật ngay", btnF, textB, iconX + iconSize + 6f, btnY + 5f);
            }
        }

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
            _lastWidth = width;
            _lastHeight = height;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int maxScroll = GetMaxScroll(height, width);
            TargetScrollY = Math.Max(0, Math.Min(maxScroll, TargetScrollY));
            if (SmoothScrollY > maxScroll) SmoothScrollY = maxScroll;
            if (SmoothScrollY < 0) SmoothScrollY = 0;

            int currentScroll = ScrollY;

            // 1. Background
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
            {
                g.FillRectangle(bgBrush, 0, 0, width, height);
            }

            // 2. LEFT SIDEBAR (White)
            RectangleF sidebarRect = new RectangleF(0, 0, SidebarWidth, height);
            using (SolidBrush sbBg = new SolidBrush(Color.White))
            using (Pen sbBorder = new Pen(Color.FromArgb(234, 236, 240), 1f))
            {
                g.FillRectangle(sbBg, sidebarRect);
                g.DrawLine(sbBorder, SidebarWidth, 0, SidebarWidth, height);
            }

            // 2.1 Logo
            GenOfficeLogoRenderer.DrawLogo(g, 20f, 14f, 30f, false);
            using (Font brandFont = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (SolidBrush brandBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString("ZENQ OFFICE", brandFont, brandBrush, 56f, 18f);
            }

            // 2.2 Nav items (Trang chủ, Tài liệu của tôi, Thùng rác)
            RectangleF tab0Rect = GetMyDocsTabBounds(width, height);
            DrawSidebarNavItem(g, tab0Rect, "Trang chủ", (gr, ix, iy, isz, icol) => DrawVectorHomeIcon(gr, ix, iy, isz, icol), ActiveTab == 0, HoverTabIndex == 0);

            RectangleF navDocRect = GetOpenDocNavBounds(width, height);
            DrawSidebarNavItem(g, navDocRect, "Tài liệu của tôi", (gr, ix, iy, isz, icol) => DrawVectorFolderIcon(gr, ix, iy, isz, icol), ActiveTab == 3, HoverTabIndex == 3 || HoverSidebarDoc);

            RectangleF tab1Rect = GetTrashTabBounds(width, height);
            DrawSidebarNavItem(g, tab1Rect, "Thùng rác", (gr, ix, iy, isz, icol) => DrawVectorTrashIcon(gr, ix, iy, isz, icol), ActiveTab == 1, HoverTabIndex == 1);

            // 2.3 "Ứng dụng" section (CLICKABLE SHORTCUTS TO CREATE DOCUMENTS)
            float appSectionY = 212f;
            using (Font secLabelFont = new Font("Segoe UI", 8.2f, FontStyle.Bold))
            using (SolidBrush secLabelBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                g.DrawString("ỨNG DỤNG", secLabelFont, secLabelBrush, 20f, appSectionY);
            }
            DrawSidebarAppNavItem(g, GetAppNavBounds(0, width, height), "Tài liệu", OfficeAppMode.Writer, HoverSidebarApp == 0);
            DrawSidebarAppNavItem(g, GetAppNavBounds(1, width, height), "Bảng tính", OfficeAppMode.Calc, HoverSidebarApp == 1);
            DrawSidebarAppNavItem(g, GetAppNavBounds(2, width, height), "Thuyết trình", OfficeAppMode.Slides, HoverSidebarApp == 2);
            DrawSidebarAppNavItem(g, GetAppNavBounds(3, width, height), "PDF", OfficeAppMode.Pdf, HoverSidebarApp == 3);
            DrawSidebarAppNavItem(g, GetAppNavBounds(4, width, height), "Đồ họa & Ảnh", OfficeAppMode.Photo, HoverSidebarApp == 4);

            // 2.4 Settings at bottom
            float sepY = height - 56f;
            using (Pen sepPen = new Pen(Color.FromArgb(234, 236, 240), 1f))
            {
                g.DrawLine(sepPen, 16f, sepY, SidebarWidth - 16f, sepY);
            }
            RectangleF tab2Rect = GetSettingsTabBounds(width, height);
            DrawSidebarNavItem(g, tab2Rect, "Cài đặt", (gr, ix, iy, isz, icol) => DrawVectorGearIcon(gr, ix + isz / 2f, iy + isz / 2f, isz, icol), ActiveTab == 2, HoverTabIndex == 2);

            // 3. CENTER CONTENT
            float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;
            GetCenterContentMetrics(width, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);

            // 3.1 Top bar: Search
            RectangleF searchBoxRect = GetSearchBoxBounds(width);
            using (GraphicsPath sbp = CreateRoundedRectanglePath(searchBoxRect, 19f))
            using (SolidBrush sbb = new SolidBrush(Color.FromArgb(245, 247, 250)))
            using (Pen sbpPen = new Pen(IsSearchFocused ? Color.FromArgb(37, 99, 235) : Color.FromArgb(226, 232, 240), IsSearchFocused ? 1.5f : 1f))
            {
                g.FillPath(sbb, sbp);
                g.DrawPath(sbpPen, sbp);
            }
            DrawVectorSearchIcon(g, searchBoxRect.X + 14f, searchBoxRect.Y + 12f, 14f, Color.FromArgb(148, 163, 184));

            float textX = searchBoxRect.X + 36f;
            float textY = searchBoxRect.Y + 10f;
            using (Font searchFont = new Font("Segoe UI", 9.2f, FontStyle.Regular))
            {
                if (string.IsNullOrEmpty(SearchKeyword))
                {
                    using (SolidBrush phBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                    {
                        g.DrawString("Tìm kiếm tệp, thư mục, mẫu...", searchFont, phBrush, textX, textY);
                    }
                    if (IsSearchFocused && CursorBlink)
                    {
                        using (Pen cp = new Pen(Color.FromArgb(37, 99, 235), 1.8f))
                        {
                            g.DrawLine(cp, textX, textY + 2, textX, textY + 16);
                        }
                    }
                }
                else
                {
                    using (SolidBrush kwBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    {
                        g.DrawString(SearchKeyword, searchFont, kwBrush, textX, textY);
                    }
                    if (IsSearchFocused && CursorBlink)
                    {
                        SizeF sz = g.MeasureString(SearchKeyword, searchFont);
                        float curX = textX + sz.Width - 3;
                        using (Pen cp = new Pen(Color.FromArgb(37, 99, 235), 1.8f))
                        {
                            g.DrawLine(cp, curX, textY + 2, curX, textY + 16);
                        }
                    }
                    DrawVectorCrossIcon(g, searchBoxRect.Right - 22f, searchBoxRect.Y + 13f, 12f, Color.FromArgb(148, 163, 184));
                }
            }

            // Top bar icons: bell, help, user avatar
            float topBarRightX = width - RightPanelWidth - 24f;
            DrawVectorBellIcon(g, topBarRightX - 120f, 24f, 18f, Color.FromArgb(100, 116, 139));

            RectangleF helpRect = GetHelpBtnBounds(width, height);
            if (HoverHelpBtn)
            {
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    g.FillEllipse(hb, helpRect);
                }
            }
            DrawVectorQuestionIcon(g, helpRect.X + helpRect.Width / 2f, helpRect.Y + helpRect.Height / 2f, 16f, Color.FromArgb(100, 116, 139));

            RectangleF updateRect = GetUpdateBtnBounds(width, height);
            using (SolidBrush avatarBg = new SolidBrush(Color.FromArgb(37, 99, 235)))
            {
                g.FillEllipse(avatarBg, updateRect.X + 4f, updateRect.Y + 4f, 28f, 28f);
            }
            using (Font avatarFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (SolidBrush avatarTxt = new SolidBrush(Color.White))
            {
                StringFormat asf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                RectangleF avatarTextRect = new RectangleF(updateRect.X + 4f, updateRect.Y + 4f, 28f, 28f);
                g.DrawString("UH", avatarFont, avatarTxt, avatarTextRect, asf);
            }

            // 4. WELCOME BANNER (Tab 0 only)
            if (ActiveTab == 0)
            {
                RectangleF bannerRect = new RectangleF(startX, 68f, contentW, bannerH);
                if (!AppIconRenderer.DrawBanner(g, bannerRect))
                {
                    using (GraphicsPath bp = CreateRoundedRectanglePath(bannerRect, 16f))
                    using (LinearGradientBrush bannerBg = new LinearGradientBrush(bannerRect, Color.FromArgb(79, 102, 207), Color.FromArgb(105, 126, 218), 135f))
                    {
                        g.FillPath(bannerBg, bp);
                    }
                    using (Font bannerTitleFont = new Font("Segoe UI", 16f, FontStyle.Bold))
                    using (Font bannerSubFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                    using (SolidBrush bannerTxtBrush = new SolidBrush(Color.White))
                    {
                        g.DrawString("Chào mừng bạn quay trở lại!", bannerTitleFont, bannerTxtBrush, startX + 28f, 96f);
                        using (SolidBrush bannerSubBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
                        {
                            g.DrawString("Bắt đầu công việc của bạn với các công cụ văn phòng mạnh mẽ và linh hoạt.", bannerSubFont, bannerSubBrush, startX + 28f, 128f);
                        }
                    }
                    // Floating icons (All 5 formats)
                    float iconBlockX = bannerRect.Right - 238f;
                    float iconBlockY = bannerRect.Y + 28f;
                    AppIconRenderer.DrawAppIcon(g, iconBlockX, iconBlockY, 50f, OfficeAppMode.Writer);
                    AppIconRenderer.DrawAppIcon(g, iconBlockX + 44f, iconBlockY + 14f, 50f, OfficeAppMode.Calc);
                    AppIconRenderer.DrawAppIcon(g, iconBlockX + 88f, iconBlockY - 6f, 50f, OfficeAppMode.Slides);
                    AppIconRenderer.DrawAppIcon(g, iconBlockX + 132f, iconBlockY + 16f, 46f, OfficeAppMode.Pdf);
                    AppIconRenderer.DrawAppIcon(g, iconBlockX + 176f, iconBlockY - 2f, 48f, OfficeAppMode.Photo);
                }
            }

            // 5. CREATION CARDS (Chỉ hiển thị tại Trang chủ)
            if (ActiveTab == 0)
            {
                RectangleF c0 = GetCardBounds(0, width, height);
                RectangleF c1 = GetCardBounds(1, width, height);
                RectangleF c2 = GetCardBounds(2, width, height);
                RectangleF c3 = GetCardBounds(3, width, height);
                RectangleF c4 = GetCardBounds(4, width, height);
                DrawCreationCard(g, c0, "Tạo tài liệu", "Soạn thảo văn bản", OfficeAppMode.Writer, HoverCardIndex == 0);
                DrawCreationCard(g, c1, "Tạo bảng tính", "Xử lý dữ liệu", OfficeAppMode.Calc, HoverCardIndex == 1);
                DrawCreationCard(g, c2, "Tạo thuyết trình", "Trình bày ý tưởng", OfficeAppMode.Slides, HoverCardIndex == 2);
                DrawCreationCard(g, c3, "Tạo từ PDF", "Chỉnh sửa, chuyển đổi", OfficeAppMode.Pdf, HoverCardIndex == 3);
                DrawCreationCard(g, c4, "Đồ họa & Ảnh", "Thiết kế lớp & PSD", OfficeAppMode.Photo, HoverCardIndex == 4);
            }

            // 6. MAIN CONTENT AREA
            if (ActiveTab == 2)
            {
                using (Font secHeaderFont = new Font("Segoe UI", 13f, FontStyle.Bold))
                using (SolidBrush secHeaderBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    g.DrawString("Cài đặt", secHeaderFont, secHeaderBrush, startX, 76f);
                }
                using (Pen sepPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.DrawLine(sepPen, startX, 110f, startX + contentW, 110f);
                }
                RectangleF setViewport = new RectangleF(startX, 114f, contentW, Math.Max(100, height - 118));
                GraphicsState gs = g.Save();
                g.SetClip(setViewport);
                DrawSettingsDashboard(g, startX, contentW, 120f - currentScroll);
                g.Restore(gs);
                if (maxScroll > 0)
                {
                    float trackH = setViewport.Height - 16f;
                    float thumbH = Math.Max(30f, Math.Min(trackH - 10f, trackH * (setViewport.Height / 1480f)));
                    float thumbY = setViewport.Top + 8f + (currentScroll / (float)maxScroll) * (trackH - thumbH);
                    ScrollBarBounds = new RectangleF(startX + contentW - 10f, setViewport.Top + 4f, 10f, setViewport.Height - 8f);
                    ScrollBarThumbBounds = new RectangleF(startX + contentW - 7f, thumbY, 5f, thumbH);

                    if (IsHoveringScrollBar || IsDraggingScrollBar)
                    {
                        using (GraphicsPath trackPath = CreateRoundedRectanglePath(ScrollBarBounds, 4f))
                        using (SolidBrush trackBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                        {
                            g.FillPath(trackBg, trackPath);
                        }
                    }

                    Color thumbColor = IsDraggingScrollBar ? Color.FromArgb(71, 85, 105) :
                                       (IsHoveringScrollBar ? Color.FromArgb(100, 116, 139) : Color.FromArgb(148, 163, 184));
                    using (GraphicsPath tp = CreateRoundedRectanglePath(ScrollBarThumbBounds, 2.5f))
                    using (SolidBrush tb = new SolidBrush(thumbColor))
                    {
                        g.FillPath(tb, tp);
                    }
                }
                else
                {
                    ScrollBarBounds = RectangleF.Empty;
                    ScrollBarThumbBounds = RectangleF.Empty;
                }
            }
            else
            {
                // File list (fileListY, colHeaderY, listTopY from GetCenterContentMetrics)
                if (ActiveTab == 3)
                {
                    using (Font secHeaderFont = new Font("Segoe UI", 13f, FontStyle.Bold))
                    using (SolidBrush secHeaderBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    {
                        g.DrawString("Tài liệu của tôi", secHeaderFont, secHeaderBrush, startX, fileListY - 2f);
                    }
                    using (Font subFont = new Font("Segoe UI", 8.8f, FontStyle.Regular))
                    using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                    {
                        g.DrawString("Quản lý và mở nhanh tất cả các tệp tài liệu trong máy của bạn", subFont, subBrush, startX, fileListY + 22f);
                    }

                    // Button "Mở tệp từ máy..."
                    float btnW = 160f;
                    float btnH = 32f;
                    MyDocsOpenFileBtnRect = new RectangleF(startX + contentW - btnW, fileListY - 2f, btnW, btnH);
                    using (GraphicsPath bp = CreateRoundedRectanglePath(MyDocsOpenFileBtnRect, 6f))
                    using (SolidBrush bb = new SolidBrush(HoverMyDocsOpenFileBtn ? Color.FromArgb(239, 246, 255) : Color.FromArgb(248, 250, 252)))
                    using (Pen bpPen = new Pen(HoverMyDocsOpenFileBtn ? Color.FromArgb(37, 99, 235) : Color.FromArgb(203, 213, 225), 1.2f))
                    {
                        g.FillPath(bb, bp);
                        g.DrawPath(bpPen, bp);
                    }
                    DrawVectorFolderIcon(g, MyDocsOpenFileBtnRect.X + 12f, MyDocsOpenFileBtnRect.Y + 8f, 16f, Color.FromArgb(37, 99, 235));
                    using (Font btnF = new Font("Segoe UI", 8.8f, FontStyle.Bold))
                    using (SolidBrush btnB = new SolidBrush(Color.FromArgb(30, 64, 175)))
                    {
                        g.DrawString("Mở tệp từ máy...", btnF, btnB, MyDocsOpenFileBtnRect.X + 34f, MyDocsOpenFileBtnRect.Y + 7f);
                    }

                    // Filter pills
                    MyDocsFilterPills.Clear();
                    string[] labels = new string[] { "Tất cả", "Gần đây", "Đã gắn sao", "Văn bản (.docx)", "Bảng tính (.xlsx)", "Thuyết trình (.pptx)", "PDF (.pdf)", "Đồ họa Vector (.svg)" };
                    int[] ids = new int[] { 0, 1, 2, 10, 11, 12, 13, 15 };
                    float curPillX = startX;
                    float pillY = fileListY + 48f;
                    using (Font pillFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                    {
                        for (int p = 0; p < labels.Length; p++)
                        {
                            SizeF sz = g.MeasureString(labels[p], pillFont);
                            float pw = sz.Width + 18f;
                            RectangleF pRect = new RectangleF(curPillX, pillY, pw, 26f);
                            bool isPillActive = (SubFilter == ids[p]);
                            bool isPillHover = (HoverPillIndex == p);

                            using (GraphicsPath pp = CreateRoundedRectanglePath(pRect, 13f))
                            using (SolidBrush pb = new SolidBrush(isPillActive ? Color.FromArgb(37, 99, 235) : (isPillHover ? Color.FromArgb(241, 245, 249) : Color.White)))
                            using (Pen pPen = new Pen(isPillActive ? Color.FromArgb(37, 99, 235) : Color.FromArgb(226, 232, 240), 1f))
                            using (SolidBrush txtB = new SolidBrush(isPillActive ? Color.White : (isPillHover ? Color.FromArgb(15, 23, 42) : Color.FromArgb(100, 116, 139))))
                            {
                                g.FillPath(pb, pp);
                                g.DrawPath(pPen, pp);
                                g.DrawString(labels[p], pillFont, txtB, pRect.X + 9f, pRect.Y + 5f);
                            }
                            MyDocsFilterPills.Add(new FilterPill { Label = labels[p], FilterId = ids[p], Bounds = pRect });
                            curPillX += pw + 8f;
                        }
                    }
                }
                else
                {
                    using (Font secHeaderFont = new Font("Segoe UI", 12f, FontStyle.Bold))
                    using (SolidBrush secHeaderBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    {
                        string headerText = (ActiveTab == 1) ? "Thùng rác" :
                                            ((SubFilter == 2) ? "Tài liệu đã gắn dấu sao" :
                                            ((SubFilter == 1) ? "Tệp gần đây" : "Tất cả tệp gần đây"));
                        g.DrawString(headerText, secHeaderFont, secHeaderBrush, startX, fileListY);
                    }
                    if (ActiveTab == 0)
                    {
                        using (Font linkFont = new Font("Segoe UI", 9f, FontStyle.Regular))
                        using (SolidBrush linkBrush = new SolidBrush(Color.FromArgb(37, 99, 235)))
                        {
                            string linkText = "Xem tất cả →";
                            SizeF linkSz = g.MeasureString(linkText, linkFont);
                            g.DrawString(linkText, linkFont, linkBrush, startX + contentW - linkSz.Width, fileListY + 4f);
                        }
                    }
                    else if (ActiveTab == 1)
                    {
                        if (TrashDocs != null && TrashDocs.Count > 0)
                        {
                            RectangleF empBtn = new RectangleF(startX + contentW - 155f, fileListY - 2f, 155f, 30f);
                            using (GraphicsPath ep = CreateRoundedRectanglePath(empBtn, 4f))
                            using (SolidBrush eb = new SolidBrush(Color.FromArgb(254, 242, 242)))
                            using (Pen epPen = new Pen(Color.FromArgb(254, 202, 202), 1f))
                            {
                                g.FillPath(eb, ep);
                                g.DrawPath(epPen, ep);
                            }
                            DrawVectorTrashIcon(g, empBtn.X + 10f, empBtn.Y + 6f, 16f, Color.FromArgb(220, 38, 38));
                            using (Font btnF = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                            using (SolidBrush btnB = new SolidBrush(Color.FromArgb(220, 38, 38)))
                            {
                                g.DrawString("Dọn sạch thùng rác", btnF, btnB, empBtn.X + 30f, empBtn.Y + 7f);
                            }
                        }
                    }
                }

                // Column headers (colHeaderY and listTopY already computed from GetCenterContentMetrics)
                using (Font thFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (SolidBrush thBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString("Tên tệp", thFont, thBrush, startX + 16f, colHeaderY);
                    g.DrawString("Loại", thFont, thBrush, startX + contentW * 0.50f, colHeaderY);
                    g.DrawString("Thời gian sửa đổi", thFont, thBrush, startX + contentW * 0.62f, colHeaderY);
                    g.DrawString("Kích thước", thFont, thBrush, startX + contentW * 0.80f, colHeaderY);
                }
                using (Pen hLine = new Pen(Color.FromArgb(234, 236, 240), 1f))
                {
                    g.DrawLine(hLine, startX, colHeaderY + 20f, startX + contentW, colHeaderY + 20f);
                }

                RectangleF listViewport = new RectangleF(startX, listTopY, contentW, Math.Max(100, height - listTopY - 4f));
                GraphicsState gs = g.Save();
                g.SetClip(listViewport);

                List<RecentDocItem> currentList = GetFilteredDocs(ActiveTab);
                float startFileY = listTopY - currentScroll;
                int rowHeight = 56;
                int rowGap = 2;
                int rowTotalH = rowHeight + rowGap;

                if (currentList.Count == 0)
                {
                    using (Font emptyFont = new Font("Segoe UI", 10.5f, FontStyle.Regular))
                    using (SolidBrush emptyBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                    {
                        string emptyMsg;
                        if (!string.IsNullOrEmpty(SearchKeyword))
                        {
                            emptyMsg = string.Format("Không tìm thấy tài liệu nào khớp với từ khóa '{0}'.", SearchKeyword);
                        }
                        else
                        {
                            if (ActiveTab == 3)
                            {
                                if (SubFilter == 2 || SubFilter == 14)
                                    emptyMsg = "Chưa có tài liệu nào được gắn dấu sao. Hãy nhấp vào biểu tượng ngôi sao ở danh sách tệp để đánh dấu tài liệu quan trọng!";
                                else
                                    emptyMsg = "Chưa có tài liệu nào trong mục này. Nhấn 'Mở tệp từ máy...' để thêm tài liệu hoặc tạo tệp mới từ Trang chủ!";
                            }
                            else if (ActiveTab == 0)
                            {
                                emptyMsg = "Chưa có tệp nào gần đây. Hãy tạo tệp mới từ các chức năng ở trên!";
                            }
                            else
                            {
                                emptyMsg = "Thùng rác đang trống.";
                            }
                        }
                        g.DrawString(emptyMsg, emptyFont, emptyBrush, startX + 16f, startFileY + 24f);
                    }
                }
                else
                {
                    using (Font rTitleFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                    using (Font rPathFont = new Font("Segoe UI", 8f, FontStyle.Regular))
                    using (Font rFont = new Font("Segoe UI", 8.8f, FontStyle.Regular))
                    using (Font badgeFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                    {
                        for (int i = 0; i < currentList.Count; i++)
                        {
                            RecentDocItem doc = currentList[i];
                            float rowY = startFileY + i * rowTotalH;
                            if (rowY + rowHeight < listViewport.Top - 30 || rowY > listViewport.Bottom + 30) continue;

                            RectangleF rowRect = new RectangleF(startX, rowY, contentW, rowHeight);
                            bool isRowHover = (HoverRowIndex == i);

                            Color accentColor = (doc.Mode == OfficeAppMode.Writer) ? Color.FromArgb(37, 99, 235) :
                                                ((doc.Mode == OfficeAppMode.Calc) ? Color.FromArgb(16, 185, 129) :
                                                ((doc.Mode == OfficeAppMode.Slides) ? Color.FromArgb(249, 115, 22) :
                                                ((doc.Mode == OfficeAppMode.Photo) ? Color.FromArgb(99, 102, 241) : Color.FromArgb(220, 38, 38))));

                            if (isRowHover)
                            {
                                using (SolidBrush rb = new SolidBrush(Color.FromArgb(248, 250, 252)))
                                {
                                    g.FillRectangle(rb, rowRect);
                                }
                            }
                            using (Pen rLine = new Pen(Color.FromArgb(241, 245, 249), 1f))
                            {
                                g.DrawLine(rLine, rowRect.X, rowRect.Bottom, rowRect.Right, rowRect.Bottom);
                            }

                            // Doc icon
                            DrawVectorAppEmblem(g, rowRect.X + 14f, rowRect.Y + 12f, 32f, doc.Mode);

                            // Title (Bound to column 1 with ellipsis to avoid overlapping column 2)
                            float colLoaiX = startX + contentW * 0.50f;
                            float titleMaxW = Math.Max(80f, colLoaiX - (rowRect.X + 56f) - 16f);
                            RectangleF titleRect = new RectangleF(rowRect.X + 56f, rowRect.Y + 9f, titleMaxW, 20f);
                            using (SolidBrush titleB = new SolidBrush(Color.FromArgb(15, 23, 42)))
                            using (StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center })
                            {
                                g.DrawString(doc.Title, rTitleFont, titleB, titleRect, sf);
                            }
                            // Path
                            string displayPath = !string.IsNullOrEmpty(doc.FilePath) ? doc.FilePath : "(Bản nháp tự động lưu)";
                            RectangleF pathRect = new RectangleF(rowRect.X + 56f, rowRect.Y + 31f, titleMaxW, 16f);
                            using (SolidBrush pathB = new SolidBrush(Color.FromArgb(148, 163, 184)))
                            using (StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisPath, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center })
                            {
                                g.DrawString(displayPath, rPathFont, pathB, pathRect, sf);
                            }

                            // Type badge pill
                            string modeName = (doc.Mode == OfficeAppMode.Writer) ? "Tài liệu" :
                                              ((doc.Mode == OfficeAppMode.Calc) ? "Bảng tính" :
                                              ((doc.Mode == OfficeAppMode.Slides) ? "Thuyết trình" :
                                              ((doc.Mode == OfficeAppMode.Photo) ? "Đồ họa PSD" : "PDF")));
                            SizeF badgeSz = g.MeasureString(modeName, badgeFont);
                            float badgeW = badgeSz.Width + 16f;
                            RectangleF badgeRect = new RectangleF(startX + contentW * 0.50f - 4f, rowRect.Y + 17f, badgeW, 22f);
                            using (GraphicsPath bp = CreateRoundedRectanglePath(badgeRect, 4f))
                            using (SolidBrush bb = new SolidBrush(Color.FromArgb(25, accentColor.R, accentColor.G, accentColor.B)))
                            {
                                g.FillPath(bb, bp);
                            }
                            using (SolidBrush bt = new SolidBrush(accentColor))
                            {
                                StringFormat bsf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                                g.DrawString(modeName, badgeFont, bt, badgeRect, bsf);
                            }

                            // Last modified
                            float dateW = contentW * 0.16f;
                            RectangleF dateRect = new RectangleF(startX + contentW * 0.62f, rowRect.Y + 17f, dateW, 22f);
                            using (SolidBrush timeB = new SolidBrush(Color.FromArgb(100, 116, 139)))
                            using (StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center })
                            {
                                g.DrawString(doc.LastModified, rFont, timeB, dateRect, sf);
                            }
                            // Size placeholder
                            using (SolidBrush sizeB = new SolidBrush(Color.FromArgb(100, 116, 139)))
                            {
                                g.DrawString("—", rFont, sizeB, startX + contentW * 0.80f, rowRect.Y + 18f);
                            }

                            // Action
                            if (ActiveTab == 0 || ActiveTab == 3)
                            {
                                float dotX = startX + contentW - 24f;
                                float dotCY = rowRect.Y + rowHeight / 2f;

                                // Star Icon
                                float starX = dotX - 32f;
                                bool isRowStarHov = (HoverRowStar == i);
                                Color starColor = doc.IsStarred ? Color.FromArgb(245, 158, 11) :
                                                  (isRowStarHov ? Color.FromArgb(245, 158, 11) : Color.FromArgb(203, 213, 225));
                                DrawVectorStarIcon(g, starX, dotCY, 7.5f, starColor, doc.IsStarred);

                                // 3 dots
                                using (SolidBrush dotB = new SolidBrush(Color.FromArgb(148, 163, 184)))
                                {
                                    g.FillEllipse(dotB, dotX, dotCY - 6f, 3f, 3f);
                                    g.FillEllipse(dotB, dotX, dotCY - 0.5f, 3f, 3f);
                                    g.FillEllipse(dotB, dotX, dotCY + 5f, 3f, 3f);
                                }
                            }
                            else
                            {
                                RectangleF resBtn = new RectangleF(startX + contentW - 135f, rowRect.Y + 14f, 65f, 28f);
                                using (GraphicsPath rbp = CreateRoundedRectanglePath(resBtn, 4f))
                                using (SolidBrush rbb = new SolidBrush(Color.FromArgb(236, 253, 245)))
                                using (Pen rbPen = new Pen(Color.FromArgb(167, 243, 208), 1f))
                                {
                                    g.FillPath(rbb, rbp);
                                    g.DrawPath(rbPen, rbp);
                                }
                                using (SolidBrush rtBrush = new SolidBrush(Color.FromArgb(5, 150, 105)))
                                {
                                    StringFormat rsf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                                    g.DrawString("Phục hồi", rFont, rtBrush, resBtn, rsf);
                                }
                                RectangleF purgeBtn = new RectangleF(startX + contentW - 60f, rowRect.Y + 14f, 50f, 28f);
                                using (GraphicsPath pbp = CreateRoundedRectanglePath(purgeBtn, 4f))
                                using (SolidBrush pbb = new SolidBrush(Color.FromArgb(254, 242, 242)))
                                using (Pen pbPen = new Pen(Color.FromArgb(254, 202, 202), 1f))
                                {
                                    g.FillPath(pbb, pbp);
                                    g.DrawPath(pbPen, pbp);
                                }
                                using (SolidBrush ptBrush = new SolidBrush(Color.FromArgb(220, 38, 38)))
                                {
                                    StringFormat psf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                                    g.DrawString("Xóa", rFont, ptBrush, purgeBtn, psf);
                                }
                            }
                        }
                    }
                }
                g.Restore(gs);

                if (maxScroll > 0)
                {
                    float trackH = listViewport.Height - 16f;
                    float totalListH = currentList.Count * rowTotalH + 96f;
                    float thumbH = Math.Max(30f, Math.Min(trackH - 10f, trackH * (listViewport.Height / totalListH)));
                    float thumbY = listViewport.Top + 8f + (currentScroll / (float)maxScroll) * (trackH - thumbH);
                    ScrollBarBounds = new RectangleF(startX + contentW - 10f, listViewport.Top + 4f, 10f, listViewport.Height - 8f);
                    ScrollBarThumbBounds = new RectangleF(startX + contentW - 7f, thumbY, 5f, thumbH);

                    if (IsHoveringScrollBar || IsDraggingScrollBar)
                    {
                        using (GraphicsPath trackPath = CreateRoundedRectanglePath(ScrollBarBounds, 4f))
                        using (SolidBrush trackBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                        {
                            g.FillPath(trackBg, trackPath);
                        }
                    }

                    Color thumbColor = IsDraggingScrollBar ? Color.FromArgb(71, 85, 105) :
                                       (IsHoveringScrollBar ? Color.FromArgb(100, 116, 139) : Color.FromArgb(148, 163, 184));
                    using (GraphicsPath tp = CreateRoundedRectanglePath(ScrollBarThumbBounds, 2.5f))
                    using (SolidBrush tb = new SolidBrush(thumbColor))
                    {
                        g.FillPath(tb, tp);
                    }
                }
                else
                {
                    ScrollBarBounds = RectangleF.Empty;
                    ScrollBarThumbBounds = RectangleF.Empty;
                }
            }

            // 7. RIGHT SIDEBAR PANEL
            if (ActiveTab != 2)
            {
                DrawRightSidebarPanel(g, width, height);
            }
        }

        private void DrawWpsAppCard(Graphics g, RectangleF rect, string name, string subtitle, OfficeAppMode mode, bool isHover)
        {
            Color accentColor;
            if (mode == OfficeAppMode.Writer)
            {
                accentColor = Color.FromArgb(37, 99, 235);
            }
            else if (mode == OfficeAppMode.Calc)
            {
                accentColor = Color.FromArgb(16, 185, 129);
            }
            else if (mode == OfficeAppMode.Slides)
            {
                accentColor = Color.FromArgb(249, 115, 22);
            }
            else
            {
                accentColor = Color.FromArgb(220, 38, 38);
            }

            // Nền thẻ trắng phẳng chuẩn WPS (#FFFFFF)
            using (GraphicsPath cp = CreateRoundedRectanglePath(rect, 8f))
            using (SolidBrush cb = new SolidBrush(Color.White))
            using (Pen cPen = new Pen(isHover ? accentColor : Color.FromArgb(226, 232, 240), isHover ? 1.5f : 1f))
            {
                if (isHover)
                {
                    RectangleF shadow = new RectangleF(rect.X, rect.Y + 2f, rect.Width, rect.Height);
                    using (GraphicsPath sp = CreateRoundedRectanglePath(shadow, 8f))
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(18, 0, 0, 0)))
                    {
                        g.FillPath(sb, sp);
                    }
                }

                g.FillPath(cb, cp);
                g.DrawPath(cPen, cp);
            }

            // Biểu tượng ứng dụng Vector nguyên bản, sắc nét & chuẩn thương hiệu
            DrawVectorAppEmblem(g, rect.X + 14f, rect.Y + 12f, 36f, mode);

            // Tên ứng dụng
            using (Font nameFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (SolidBrush nameB = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString(name, nameFont, nameB, rect.X + 14f, rect.Y + 54f);
            }

            // Phụ đề mô tả
            using (Font subFont = new Font("Segoe UI", 8.2f, FontStyle.Regular))
            using (SolidBrush subB = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                g.DrawString(subtitle, subFont, subB, rect.X + 14f, rect.Y + 73f);
            }
        }

        private void DrawSettingsDashboard(Graphics g, float startX, float contentW, float curY)
        {
            using (Font secFont = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (Font rTitleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font ctrlFont = new Font("Segoe UI", 8.8f, FontStyle.Regular))
            using (SolidBrush secBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (SolidBrush rTitleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (Pen sepPen = new Pen(Color.FromArgb(241, 245, 249), 1f))
            {
                // ================= SECTION 1: TRÍ TUỆ NHÂN TẠO =================
                DrawVectorSparkleIcon(g, startX, curY + 2f, 16, Color.FromArgb(99, 102, 241));
                g.DrawString("Trí tuệ nhân tạo", secFont, secBrush, startX + 22f, curY);
                curY += 26f;

                float card1H = 150f;
                RectangleF card1 = new RectangleF(startX, curY, contentW, card1H);
                DrawWpsCardContainer(g, card1);

                // Row 1: AI Copilot Toggle
                float r1Y = card1.Y;
                g.DrawString("Trợ lý AI Copilot", rTitleFont, rTitleBrush, card1.X + 16f, r1Y + 15f);
                SettingAiCopilotToggleRect = new RectangleF(card1.Right - 95f, r1Y + 11f, 80f, 28f);
                DrawWpsToggleSwitch(g, SettingAiCopilotToggleRect, AppSettings.Instance.AiCopilotEnabled);

                g.DrawLine(sepPen, card1.X + 16f, r1Y + 50f, card1.Right - 16f, r1Y + 50f);

                // Row 2: AI Provider
                float r2Y = card1.Y + 50f;
                g.DrawString("Mô hình AI", rTitleFont, rTitleBrush, card1.X + 16f, r2Y + 15f);
                int prov = AppSettings.Instance.SelectedAiProvider;
                SettingAiProviderGoogleRect = new RectangleF(card1.Right - 465f, r2Y + 10f, 115f, 30f);
                SettingAiProviderOpenAiRect = new RectangleF(card1.Right - 340f, r2Y + 10f, 110f, 30f);
                SettingAiProviderClaudeRect = new RectangleF(card1.Right - 220f, r2Y + 10f, 105f, 30f);
                SettingAiProviderOllamaRect = new RectangleF(card1.Right - 105f, r2Y + 10f, 90f, 30f);

                DrawWpsSegmentedButton(g, SettingAiProviderGoogleRect, "Google Gemini", prov == 0, ctrlFont);
                DrawWpsSegmentedButton(g, SettingAiProviderOpenAiRect, "OpenAI GPT", prov == 1, ctrlFont);
                DrawWpsSegmentedButton(g, SettingAiProviderClaudeRect, "Claude 3.5", prov == 2, ctrlFont);
                DrawWpsSegmentedButton(g, SettingAiProviderOllamaRect, "Ollama Local", prov == 3, ctrlFont);

                g.DrawLine(sepPen, card1.X + 16f, r2Y + 50f, card1.Right - 16f, r2Y + 50f);

                // Row 3: API Key & Dialog Button
                float r3Y = card1.Y + 100f;
                g.DrawString("Cấu hình API Key", rTitleFont, rTitleBrush, card1.X + 16f, r3Y + 15f);
                SettingAiConfigBtnRect = new RectangleF(card1.Right - 165f, r3Y + 9f, 150f, 32f);
                DrawWpsActionButton(g, SettingAiConfigBtnRect, "Thiết lập API Key...", Color.FromArgb(79, 70, 229), ctrlFont, '\uE8D7');

                curY += card1H + 20f;

                // ================= SECTION 2: GIAO DIỆN =================
                DrawFluentPaletteIcon(g, startX, curY + 2f, 16f, Color.FromArgb(37, 99, 235));
                g.DrawString("Giao diện", secFont, secBrush, startX + 22f, curY);
                curY += 26f;

                float card2H = 100f;
                RectangleF card2 = new RectangleF(startX, curY, contentW, card2H);
                DrawWpsCardContainer(g, card2);

                // Row 1: Ribbon Style
                float c2r1Y = card2.Y;
                g.DrawString("Kiểu thanh công cụ", rTitleFont, rTitleBrush, card2.X + 16f, c2r1Y + 15f);
                bool isIsland = (AppSettings.Instance.RibbonStyle == RibbonStyle.Island);
                SettingRibbonIslandRect = new RectangleF(card2.Right - 280f, c2r1Y + 10f, 130f, 30f);
                SettingRibbonStandardRect = new RectangleF(card2.Right - 140f, c2r1Y + 10f, 125f, 30f);
                DrawWpsSegmentedButton(g, SettingRibbonIslandRect, "Dạng Đảo (Island)", isIsland, ctrlFont);
                DrawWpsSegmentedButton(g, SettingRibbonStandardRect, "Tiêu chuẩn (Classic)", !isIsland, ctrlFont);

                g.DrawLine(sepPen, card2.X + 16f, c2r1Y + 50f, card2.Right - 16f, c2r1Y + 50f);

                // Row 2: Theme
                float c2r2Y = card2.Y + 50f;
                g.DrawString("Chủ đề giao diện", rTitleFont, rTitleBrush, card2.X + 16f, c2r2Y + 15f);
                bool isLight = (AppSettings.Instance.Theme == AppTheme.Light);
                SettingThemeLightRect = new RectangleF(card2.Right - 230f, c2r2Y + 10f, 105f, 30f);
                SettingThemeDarkRect = new RectangleF(card2.Right - 115f, c2r2Y + 10f, 100f, 30f);
                DrawWpsSegmentedButton(g, SettingThemeLightRect, "Sáng (Light)", isLight, ctrlFont);
                DrawWpsSegmentedButton(g, SettingThemeDarkRect, "Tối (Dark)", !isLight, ctrlFont);

                curY += card2H + 20f;

                // ================= SECTION: LỐI TẮT MÀN HÌNH CHÍNH =================
                DrawVectorDesktopIcon(g, startX, curY + 2f, 16f, Color.FromArgb(14, 165, 233));
                g.DrawString("Lối tắt màn hình", secFont, secBrush, startX + 22f, curY);
                curY += 26f;

                float cardShortcutsH = 200f;
                RectangleF cardSc = new RectangleF(startX, curY, contentW, cardShortcutsH);
                DrawWpsCardContainer(g, cardSc);

                float scRowH = 50f;
                float scRowY = cardSc.Y;

                // Row 1: Writer Shortcut
                AppIconRenderer.DrawAppIcon(g, cardSc.X + 16f, scRowY + 9f, 32f, OfficeAppMode.Writer);
                g.DrawString("ZenQ Writer", rTitleFont, rTitleBrush, cardSc.X + 58f, scRowY + 15f);
                SettingShortcutWriterRect = new RectangleF(cardSc.Right - 95f, scRowY + 11f, 80f, 28f);
                bool hasWriterSc = DesktopShortcutManager.IsShortcutExists("ZenQ Writer") || DesktopShortcutManager.IsShortcutExists("GenOffice Writer");
                DrawWpsToggleSwitch(g, SettingShortcutWriterRect, hasWriterSc);
                g.DrawLine(sepPen, cardSc.X + 16f, scRowY + scRowH, cardSc.Right - 16f, scRowY + scRowH);
                scRowY += scRowH;

                // Row 2: Calc Shortcut
                AppIconRenderer.DrawAppIcon(g, cardSc.X + 16f, scRowY + 9f, 32f, OfficeAppMode.Calc);
                g.DrawString("ZenQ Calc", rTitleFont, rTitleBrush, cardSc.X + 58f, scRowY + 15f);
                SettingShortcutCalcRect = new RectangleF(cardSc.Right - 95f, scRowY + 11f, 80f, 28f);
                bool hasCalcSc = DesktopShortcutManager.IsShortcutExists("ZenQ Calc") || DesktopShortcutManager.IsShortcutExists("GenOffice Calc");
                DrawWpsToggleSwitch(g, SettingShortcutCalcRect, hasCalcSc);
                g.DrawLine(sepPen, cardSc.X + 16f, scRowY + scRowH, cardSc.Right - 16f, scRowY + scRowH);
                scRowY += scRowH;

                // Row 3: Slides Shortcut
                AppIconRenderer.DrawAppIcon(g, cardSc.X + 16f, scRowY + 9f, 32f, OfficeAppMode.Slides);
                g.DrawString("ZenQ Slides", rTitleFont, rTitleBrush, cardSc.X + 58f, scRowY + 15f);
                SettingShortcutSlidesRect = new RectangleF(cardSc.Right - 95f, scRowY + 11f, 80f, 28f);
                bool hasSlidesSc = DesktopShortcutManager.IsShortcutExists("ZenQ Slides") || DesktopShortcutManager.IsShortcutExists("GenOffice Slides");
                DrawWpsToggleSwitch(g, SettingShortcutSlidesRect, hasSlidesSc);
                g.DrawLine(sepPen, cardSc.X + 16f, scRowY + scRowH, cardSc.Right - 16f, scRowY + scRowH);
                scRowY += scRowH;

                // Row 4: PDF Shortcut
                AppIconRenderer.DrawAppIcon(g, cardSc.X + 16f, scRowY + 9f, 32f, OfficeAppMode.Pdf);
                g.DrawString("ZenQ PDF", rTitleFont, rTitleBrush, cardSc.X + 58f, scRowY + 15f);
                SettingShortcutPdfRect = new RectangleF(cardSc.Right - 95f, scRowY + 11f, 80f, 28f);
                bool hasPdfSc = DesktopShortcutManager.IsShortcutExists("ZenQ PDF") || DesktopShortcutManager.IsShortcutExists("GenOffice PDF");
                DrawWpsToggleSwitch(g, SettingShortcutPdfRect, hasPdfSc);

                curY += cardShortcutsH + 20f;

                // ================= SECTION: ỨNG DỤNG MẶC ĐỊNH =================
                DrawVectorDefaultAppIcon(g, startX, curY + 2f, 16f, Color.FromArgb(16, 185, 129));
                g.DrawString("Ứng dụng mặc định", secFont, secBrush, startX + 22f, curY);
                using (Font subSecFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (SolidBrush subSecBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    g.DrawString("— Tự động mở tệp văn phòng (.docx, .xlsx, .pptx, .pdf) bằng ZenQ Office", subSecFont, subSecBrush, startX + 160f, curY + 2f);
                }
                curY += 26f;

                float cardAssocH = 258f;
                RectangleF cardAssoc = new RectangleF(startX, curY, contentW, cardAssocH);
                DrawWpsCardContainer(g, cardAssoc);

                float asRowH = 52f;
                float asRowY = cardAssoc.Y;

                using (Font subExtFont = new Font("Segoe UI", 8.2f, FontStyle.Regular))
                using (SolidBrush subExtBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    // Row 1: Writer
                    AppIconRenderer.DrawAppIcon(g, cardAssoc.X + 16f, asRowY + 10f, 32f, OfficeAppMode.Writer);
                    g.DrawString("ZenQ Writer", rTitleFont, rTitleBrush, cardAssoc.X + 58f, asRowY + 8f);
                    g.DrawString("Mặc định cho văn bản: .docx, .doc, .rtf, .txt, .dotx", subExtFont, subExtBrush, cardAssoc.X + 58f, asRowY + 28f);
                    SettingAssocWriterRect = new RectangleF(cardAssoc.Right - 95f, asRowY + 12f, 80f, 28f);
                    bool isWriterDef = FileAssociationManager.IsAppAssociated("-word");
                    DrawWpsToggleSwitch(g, SettingAssocWriterRect, isWriterDef);
                    g.DrawLine(sepPen, cardAssoc.X + 16f, asRowY + asRowH, cardAssoc.Right - 16f, asRowY + asRowH);
                    asRowY += asRowH;

                    // Row 2: Calc
                    AppIconRenderer.DrawAppIcon(g, cardAssoc.X + 16f, asRowY + 10f, 32f, OfficeAppMode.Calc);
                    g.DrawString("ZenQ Calc", rTitleFont, rTitleBrush, cardAssoc.X + 58f, asRowY + 8f);
                    g.DrawString("Mặc định cho bảng tính: .xlsx, .xls, .csv, .tsv, .ods", subExtFont, subExtBrush, cardAssoc.X + 58f, asRowY + 28f);
                    SettingAssocCalcRect = new RectangleF(cardAssoc.Right - 95f, asRowY + 12f, 80f, 28f);
                    bool isCalcDef = FileAssociationManager.IsAppAssociated("-calc");
                    DrawWpsToggleSwitch(g, SettingAssocCalcRect, isCalcDef);
                    g.DrawLine(sepPen, cardAssoc.X + 16f, asRowY + asRowH, cardAssoc.Right - 16f, asRowY + asRowH);
                    asRowY += asRowH;

                    // Row 3: Slides
                    AppIconRenderer.DrawAppIcon(g, cardAssoc.X + 16f, asRowY + 10f, 32f, OfficeAppMode.Slides);
                    g.DrawString("ZenQ Slides", rTitleFont, rTitleBrush, cardAssoc.X + 58f, asRowY + 8f);
                    g.DrawString("Mặc định cho trình chiếu: .pptx, .ppt, .ppsx, .potx", subExtFont, subExtBrush, cardAssoc.X + 58f, asRowY + 28f);
                    SettingAssocSlidesRect = new RectangleF(cardAssoc.Right - 95f, asRowY + 12f, 80f, 28f);
                    bool isSlidesDef = FileAssociationManager.IsAppAssociated("-slides");
                    DrawWpsToggleSwitch(g, SettingAssocSlidesRect, isSlidesDef);
                    g.DrawLine(sepPen, cardAssoc.X + 16f, asRowY + asRowH, cardAssoc.Right - 16f, asRowY + asRowH);
                    asRowY += asRowH;

                    // Row 4: PDF
                    AppIconRenderer.DrawAppIcon(g, cardAssoc.X + 16f, asRowY + 10f, 32f, OfficeAppMode.Pdf);
                    g.DrawString("ZenQ PDF", rTitleFont, rTitleBrush, cardAssoc.X + 58f, asRowY + 8f);
                    bool isPdfDef = FileAssociationManager.IsAppAssociated("-pdf");
                    string pdfDesc = isPdfDef
                        ? "Đã đặt làm ứng dụng mặc định (.pdf)"
                        : "Mặc định cho tài liệu PDF: .pdf (Nhấn để bật mặc định)";
                    g.DrawString(pdfDesc, subExtFont, isPdfDef ? rTitleBrush : subExtBrush, cardAssoc.X + 58f, asRowY + 28f);
                    SettingAssocPdfRect = new RectangleF(cardAssoc.Right - 95f, asRowY + 12f, 80f, 28f);
                    DrawWpsToggleSwitch(g, SettingAssocPdfRect, isPdfDef);
                    g.DrawLine(sepPen, cardAssoc.X + 16f, asRowY + asRowH, cardAssoc.Right - 16f, asRowY + asRowH);
                    asRowY += asRowH;

                    // Row 5: Master Actions
                    g.DrawString("Tác vụ hệ thống:", rTitleFont, rTitleBrush, cardAssoc.X + 16f, asRowY + 16f);
                    SettingAssocSetAllBtnRect = new RectangleF(cardAssoc.Right - 475f, asRowY + 9f, 150f, 32f);
                    SettingAssocUnsetAllBtnRect = new RectangleF(cardAssoc.Right - 315f, asRowY + 9f, 140f, 32f);
                    SettingAssocWinSettingsRect = new RectangleF(cardAssoc.Right - 165f, asRowY + 9f, 150f, 32f);

                    DrawWpsActionButton(g, SettingAssocSetAllBtnRect, "Đặt tất cả mặc định", Color.FromArgb(16, 185, 129), ctrlFont, '\uE73E');
                    DrawWpsActionButton(g, SettingAssocUnsetAllBtnRect, "Tắt tất cả mặc định", Color.FromArgb(100, 116, 139), ctrlFont, '\uE711');
                    DrawWpsActionButton(g, SettingAssocWinSettingsRect, "Cài đặt Windows...", Color.FromArgb(37, 99, 235), ctrlFont, '\uE713');
                }

                curY += cardAssocH + 20f;

                // ================= SECTION 3: HỆ THỐNG & SOẠN THẢO =================
                DrawFluentSlidersIcon(g, startX, curY + 2f, 16f, Color.FromArgb(5, 150, 105));
                g.DrawString("Hệ thống & Soạn thảo", secFont, secBrush, startX + 22f, curY);
                curY += 26f;

                float card3H = 300f;
                RectangleF card3 = new RectangleF(startX, curY, contentW, card3H);
                DrawWpsCardContainer(g, card3);

                float r3RowH = 50f;
                float r3YCur = card3.Y;

                // Row 1: Auto-Save
                g.DrawString("Tự động lưu", rTitleFont, rTitleBrush, card3.X + 16f, r3YCur + 15f);
                SettingAutoSaveToggleRect = new RectangleF(card3.Right - 95f, r3YCur + 11f, 80f, 28f);
                DrawWpsToggleSwitch(g, SettingAutoSaveToggleRect, AppSettings.Instance.AutoSaveEnabled);
                g.DrawLine(sepPen, card3.X + 16f, r3YCur + r3RowH, card3.Right - 16f, r3YCur + r3RowH);
                r3YCur += r3RowH;

                // Row 2: Auto-Save Interval
                g.DrawString("Tần suất lưu", rTitleFont, rTitleBrush, card3.X + 16f, r3YCur + 15f);
                int interval = AppSettings.Instance.AutoSaveIntervalSeconds;
                SettingAutoSave30Rect = new RectangleF(card3.Right - 275f, r3YCur + 10f, 80f, 30f);
                SettingAutoSave60Rect = new RectangleF(card3.Right - 185f, r3YCur + 10f, 100f, 30f);
                SettingAutoSave120Rect = new RectangleF(card3.Right - 75f, r3YCur + 10f, 65f, 30f);
                DrawWpsSegmentedButton(g, SettingAutoSave30Rect, "30 giây", interval == 30, ctrlFont);
                DrawWpsSegmentedButton(g, SettingAutoSave60Rect, "60s (Chuẩn)", interval == 60, ctrlFont);
                DrawWpsSegmentedButton(g, SettingAutoSave120Rect, "120s", interval == 120, ctrlFont);
                g.DrawLine(sepPen, card3.X + 16f, r3YCur + r3RowH, card3.Right - 16f, r3YCur + r3RowH);
                r3YCur += r3RowH;

                // Row 3: Smooth Scroll
                g.DrawString("Cuộn trang mượt mà", rTitleFont, rTitleBrush, card3.X + 16f, r3YCur + 15f);
                SettingSmoothScrollRect = new RectangleF(card3.Right - 95f, r3YCur + 11f, 80f, 28f);
                DrawWpsToggleSwitch(g, SettingSmoothScrollRect, AppSettings.Instance.EnableSmoothScroll);
                g.DrawLine(sepPen, card3.X + 16f, r3YCur + r3RowH, card3.Right - 16f, r3YCur + r3RowH);
                r3YCur += r3RowH;

                // Row 4: Ruler
                g.DrawString("Thanh thước đo", rTitleFont, rTitleBrush, card3.X + 16f, r3YCur + 15f);
                SettingRulerRect = new RectangleF(card3.Right - 95f, r3YCur + 11f, 80f, 28f);
                DrawWpsToggleSwitch(g, SettingRulerRect, AppSettings.Instance.ShowRuler);
                g.DrawLine(sepPen, card3.X + 16f, r3YCur + r3RowH, card3.Right - 16f, r3YCur + r3RowH);
                r3YCur += r3RowH;

                // Row 5: Auto-Correct Vietnamese
                g.DrawString("Tự động sửa lỗi gõ", rTitleFont, rTitleBrush, card3.X + 16f, r3YCur + 15f);
                SettingAutoCorrectViRect = new RectangleF(card3.Right - 95f, r3YCur + 11f, 80f, 28f);
                DrawWpsToggleSwitch(g, SettingAutoCorrectViRect, AppSettings.Instance.AutoCorrectVietnamese);
                g.DrawLine(sepPen, card3.X + 16f, r3YCur + r3RowH, card3.Right - 16f, r3YCur + r3RowH);
                r3YCur += r3RowH;

                // Row 6: Word Prediction
                g.DrawString("Gợi ý từ thông minh", rTitleFont, rTitleBrush, card3.X + 16f, r3YCur + 15f);
                SettingWordPredictionRect = new RectangleF(card3.Right - 95f, r3YCur + 11f, 80f, 28f);
                DrawWpsToggleSwitch(g, SettingWordPredictionRect, AppSettings.Instance.WordPredictionEnabled);

                curY += card3H + 20f;

                // ================= SECTION 4: THỜI GIAN & NGÔN NGỮ =================
                DrawFluentGlobeIcon(g, startX, curY + 2f, 16f, Color.FromArgb(220, 38, 38));
                g.DrawString("Ngôn ngữ", secFont, secBrush, startX + 22f, curY);
                curY += 26f;

                float card4H = 50f;
                RectangleF card4 = new RectangleF(startX, curY, contentW, card4H);
                DrawWpsCardContainer(g, card4);

                g.DrawString("Ngôn ngữ giao diện", rTitleFont, rTitleBrush, card4.X + 16f, card4.Y + 15f);
                bool isVi = (AppSettings.Instance.Language == AppLanguage.Vietnamese);
                SettingLangViRect = new RectangleF(card4.Right - 265f, card4.Y + 10f, 125f, 30f);
                SettingLangEnRect = new RectangleF(card4.Right - 130f, card4.Y + 10f, 115f, 30f);
                DrawWpsSegmentedButton(g, SettingLangViRect, "Tiếng Việt (VN)", isVi, ctrlFont);
                DrawWpsSegmentedButton(g, SettingLangEnRect, "English (US)", !isVi, ctrlFont);

                curY += card4H + 20f;

                // ================= SECTION 5: THÔNG TIN ỨNG DỤNG =================
                DrawFluentUpdateIcon(g, startX, curY + 2f, 16f, Color.FromArgb(100, 116, 139));
                g.DrawString("Thông tin ứng dụng", secFont, secBrush, startX + 22f, curY);
                curY += 26f;

                float card5H = 52f;
                RectangleF card5 = new RectangleF(startX, curY, contentW, card5H);
                DrawWpsCardContainer(g, card5);

                g.DrawString("ZENQ OFFICE Professional", rTitleFont, rTitleBrush, card5.X + 16f, card5.Y + 16f);

                SettingCheckUpdateBtnRect = new RectangleF(card5.Right - 295f, card5.Y + 10f, 145f, 32f);
                SettingHelpBtnRect = new RectangleF(card5.Right - 140f, card5.Y + 10f, 125f, 32f);
                DrawWpsActionButton(g, SettingCheckUpdateBtnRect, "Kiểm tra cập nhật", Color.FromArgb(37, 99, 235), ctrlFont, '\uE72C');
                DrawWpsActionButton(g, SettingHelpBtnRect, "Xem tài liệu", Color.FromArgb(71, 85, 105), ctrlFont, '\uE897');
                curY += card5H + 40f;
            }
        }

        private void DrawWpsCardContainer(Graphics g, RectangleF rect)
        {
            using (GraphicsPath p = CreateRoundedRectanglePath(rect, 6f))
            using (SolidBrush bg = new SolidBrush(Color.White))
            using (Pen border = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.FillPath(bg, p);
                g.DrawPath(border, p);
            }
        }

        private void DrawWpsToggleSwitch(Graphics g, RectangleF rect, bool isOn)
        {
            // Pill capsule dimensions
            float pillW = 44f;
            float pillH = 24f;
            float pillX = rect.Right - pillW;
            float pillY = rect.Y + (rect.Height - pillH) / 2f;
            RectangleF pillRect = new RectangleF(pillX, pillY, pillW, pillH);

            Color pillColor = isOn ? Color.FromArgb(37, 99, 235) : Color.FromArgb(203, 213, 225);
            using (GraphicsPath p = CreateRoundedRectanglePath(pillRect, pillH / 2f))
            using (SolidBrush bg = new SolidBrush(pillColor))
            {
                g.FillPath(bg, p);
            }

            // Circle Thumb knob
            float thumbDia = 18f;
            float thumbY = pillY + (pillH - thumbDia) / 2f;
            float thumbX = isOn ? (pillRect.Right - thumbDia - 3f) : (pillRect.X + 3f);
            RectangleF thumbRect = new RectangleF(thumbX, thumbY, thumbDia, thumbDia);

            // Subtle drop shadow for knob
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
            {
                g.FillEllipse(shadow, thumbX + 0.5f, thumbY + 1f, thumbDia, thumbDia);
            }
            using (SolidBrush whiteThumb = new SolidBrush(Color.White))
            {
                g.FillEllipse(whiteThumb, thumbRect);
            }

            // Status label to the left of the pill (Single line, NoWrap to prevent vertical splitting)
            string statusText = isOn ? "Bật" : "Tắt";
            using (Font f = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (SolidBrush txtB = new SolidBrush(isOn ? Color.FromArgb(37, 99, 235) : Color.FromArgb(100, 116, 139)))
            {
                StringFormat sf = new StringFormat { 
                    Alignment = StringAlignment.Far, 
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                RectangleF labelRect = new RectangleF(rect.X, rect.Y, Math.Max(28f, rect.Width - pillW - 6f), rect.Height);
                g.DrawString(statusText, f, txtB, labelRect, sf);
            }
        }

        private void DrawVectorDesktopIcon(Graphics g, float x, float y, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE7F4', new RectangleF(x, y, size, size), color, size * 0.72f);
        }

        private void DrawVectorDefaultAppIcon(Graphics g, float x, float y, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE71D', new RectangleF(x, y, size, size), color, size * 0.72f);
        }

        private void DrawVectorAppLogoIcon(Graphics g, float x, float y, float size)
        {
            RectangleF r = new RectangleF(x, y, size, size);
            using (GraphicsPath path = CreateRoundedRectanglePath(r, 6f))
            using (LinearGradientBrush lgb = new LinearGradientBrush(r, Color.FromArgb(79, 70, 229), Color.FromArgb(147, 51, 234), 135f))
            {
                g.FillPath(lgb, path);
            }
            using (Font f = new Font("Segoe UI", 12f, FontStyle.Bold))
            using (SolidBrush wb = new SolidBrush(Color.White))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("G", f, wb, r, sf);
            }
        }

        private void DrawWpsSegmentedButton(Graphics g, RectangleF rect, string label, bool isSelected, Font font)
        {
            using (GraphicsPath p = CreateRoundedRectanglePath(rect, 4f))
            {
                if (isSelected)
                {
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(239, 246, 255)))
                    using (Pen pen = new Pen(Color.FromArgb(37, 99, 235), 1.2f))
                    {
                        g.FillPath(bg, p);
                        g.DrawPath(pen, p);
                    }
                    using (SolidBrush txt = new SolidBrush(Color.FromArgb(37, 99, 235)))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(label, font, txt, rect, sf);
                    }
                }
                else
                {
                    using (SolidBrush bg = new SolidBrush(Color.White))
                    using (Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                    {
                        g.FillPath(bg, p);
                        g.DrawPath(pen, p);
                    }
                    using (SolidBrush txt = new SolidBrush(Color.FromArgb(71, 85, 105)))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(label, font, txt, rect, sf);
                    }
                }
            }
        }

        private void DrawWpsActionButton(Graphics g, RectangleF rect, string label, Color textColor, Font font, char glyph = '\0')
        {
            using (GraphicsPath p = CreateRoundedRectanglePath(rect, 4f))
            using (SolidBrush bg = new SolidBrush(Color.White))
            using (Pen pen = new Pen(Color.FromArgb(203, 213, 225), 1f))
            {
                g.FillPath(bg, p);
                g.DrawPath(pen, p);
            }
            if (glyph != '\0')
            {
                float iconSize = 13f;
                float iconX = rect.X + 8f;
                float iconY = rect.Y + (rect.Height - iconSize) / 2f;
                FluentIconRenderer.DrawGlyph(g, glyph, new RectangleF(iconX, iconY, iconSize, iconSize), textColor, 11f);
                using (SolidBrush txt = new SolidBrush(textColor))
                {
                    RectangleF textRect = new RectangleF(iconX + iconSize + 4f, rect.Y, rect.Width - iconSize - 12f, rect.Height);
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                    g.DrawString(label, font, txt, textRect, sf);
                }
            }
            else
            {
                using (SolidBrush txt = new SolidBrush(textColor))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(label, font, txt, rect, sf);
                }
            }
        }

        private void DrawFluentPaletteIcon(Graphics g, float x, float y, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE790', new RectangleF(x, y, size, size), color, size * 0.72f);
        }

        private void DrawFluentSlidersIcon(Graphics g, float x, float y, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE9E9', new RectangleF(x, y, size, size), color, size * 0.72f);
        }

        private void DrawFluentGlobeIcon(Graphics g, float x, float y, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE774', new RectangleF(x, y, size, size), color, size * 0.72f);
        }

        private void DrawFluentUpdateIcon(Graphics g, float x, float y, float size, Color color)
        {
            FluentIconRenderer.DrawGlyph(g, '\uE72C', new RectangleF(x, y, size, size), color, size * 0.72f);
        }

        private void DrawWin11UpdateIcon(Graphics g, float x, float y, float size, Color color)
        {
            // Sử dụng trực tiếp vector glyph chuẩn Microsoft Win11 Fluent Icons (\uE72C: Sync / Refresh)
            // Khử răng cưa SSAA 4x mượt mà, đúng chuẩn Windows 11, không tự vẽ thủ công
            FluentIconRenderer.DrawGlyph(g, '\uE72C', new RectangleF(x, y, size, size), color, size * 0.72f);
        }
        #endregion

        #region Interaction Handlers
        public bool HandleMouseMove(int x, int y, int width, int height = 800)
        {
            int prevTab = HoverTabIndex;
            int prevCard = HoverCardIndex;
            int prevRow = HoverRowIndex;
            bool prevHelp = HoverHelpBtn;
            bool prevUpdate = HoverUpdateBtn;
            bool prevSideDoc = HoverSidebarDoc;
            bool prevSideRec = HoverSidebarRecent;
            bool prevSideStar = HoverSidebarStar;
            int prevSideApp = HoverSidebarApp;
            bool prevPromo = HoverPromoBtn;
            bool prevStorage = HoverStorageManage;
            int prevTmpl = HoverTemplateIndex;
            int prevRowStar = HoverRowStar;
            bool prevUpdBtn = HoverRightPanelUpdateBtn;

            HoverTabIndex = -1;
            HoverSidebarDoc = false;
            HoverSidebarRecent = false;
            HoverSidebarStar = false;
            HoverSidebarApp = -1;
            HoverPromoBtn = false;
            HoverStorageManage = false;
            HoverTemplateIndex = -1;
            HoverRowStar = -1;
            HoverRightPanelUpdateBtn = false;

            RectangleF myDocsTab = GetMyDocsTabBounds(width, height);
            RectangleF openDocNav = GetOpenDocNavBounds(width, height);
            RectangleF trashTab = GetTrashTabBounds(width, height);
            RectangleF settingsTab = GetSettingsTabBounds(width, height);
            RectangleF helpRect = GetHelpBtnBounds(width, height);
            RectangleF updateRect = GetUpdateBtnBounds(width, height);

            if (myDocsTab.Contains(x, y)) HoverTabIndex = 0;
            else if (openDocNav.Contains(x, y)) { HoverTabIndex = 3; HoverSidebarDoc = true; }
            else if (trashTab.Contains(x, y)) HoverTabIndex = 1;
            else if (settingsTab.Contains(x, y)) HoverTabIndex = 2;

            for (int a = 0; a < 5; a++)
            {
                if (GetAppNavBounds(a, width, height).Contains(x, y))
                {
                    HoverSidebarApp = a;
                    break;
                }
            }

            HoverHelpBtn = helpRect.Contains(x, y);
            HoverUpdateBtn = updateRect.Contains(x, y);

            HoverMyDocsOpenFileBtn = (ActiveTab == 3 && MyDocsOpenFileBtnRect.Contains(x, y));
            HoverPillIndex = -1;
            if (ActiveTab == 3)
            {
                for (int p = 0; p < MyDocsFilterPills.Count; p++)
                {
                    if (MyDocsFilterPills[p].Bounds.Contains(x, y))
                    {
                        HoverPillIndex = p;
                        break;
                    }
                }
            }

            // Right Panel Hover
            if (ActiveTab != 2)
            {
                HoverPromoBtn = GetPromoBtnBounds(width).Contains(x, y);
                HoverStorageManage = GetStorageManageBounds(width).Contains(x, y);
                HoverRightPanelUpdateBtn = RightPanelUpdateBtnRect.Contains(x, y);
                for (int t = 0; t < 4; t++)
                {
                    if (GetTemplateCardBounds(t, width).Contains(x, y))
                    {
                        HoverTemplateIndex = t;
                        break;
                    }
                }
            }

            HoverCardIndex = -1;
            if (ActiveTab == 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    RectangleF cb = GetCardBounds(i, width, height);
                    if (cb.Contains(x, y))
                    {
                        HoverCardIndex = i;
                        break;
                    }
                }
            }

            HoverRowIndex = -1;
            if (ActiveTab == 0 || ActiveTab == 1 || ActiveTab == 3)
            {
                List<RecentDocItem> currentList = GetFilteredDocs(ActiveTab);
                if (currentList != null && currentList.Count > 0)
                {
                    float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;
                    GetCenterContentMetrics(width, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);
                    float startY = listTopY - ScrollY;
                    int rowHeight = 56;
                    int rowTotalH = rowHeight + 2;

                    int maxScroll = GetMaxScroll(height, width);
                    if (x >= startX && x <= startX + contentW - (maxScroll > 0 ? 14f : 0f) && y >= listTopY && y <= height - 4f)
                    {
                        float relY = y - startY;
                        if (relY >= 0)
                        {
                            int r = (int)(relY / rowTotalH);
                            float withinRowY = relY % rowTotalH;
                            if (r >= 0 && r < currentList.Count && withinRowY <= rowHeight)
                            {
                                HoverRowIndex = r;
                                if ((ActiveTab == 0 || ActiveTab == 3) && x >= startX + contentW - 54f && x <= startX + contentW - 30f)
                                {
                                    HoverRowStar = r;
                                }
                            }
                        }
                    }
                }
            }
            else if (ActiveTab == 2)
            {
                bool hovSetting = (SettingAiCopilotToggleRect.Contains(x, y) ||
                                   SettingAiProviderGoogleRect.Contains(x, y) ||
                                   SettingAiProviderOpenAiRect.Contains(x, y) ||
                                   SettingAiProviderClaudeRect.Contains(x, y) ||
                                   SettingAiProviderOllamaRect.Contains(x, y) ||
                                   SettingAiConfigBtnRect.Contains(x, y) ||
                                   SettingRibbonIslandRect.Contains(x, y) ||
                                   SettingRibbonStandardRect.Contains(x, y) ||
                                   SettingThemeLightRect.Contains(x, y) ||
                                   SettingThemeDarkRect.Contains(x, y) ||
                                   SettingAutoSaveToggleRect.Contains(x, y) ||
                                   SettingAutoSave30Rect.Contains(x, y) ||
                                   SettingAutoSave60Rect.Contains(x, y) ||
                                   SettingAutoSave120Rect.Contains(x, y) ||
                                   SettingSmoothScrollRect.Contains(x, y) ||
                                   SettingRulerRect.Contains(x, y) ||
                                   SettingAutoCorrectViRect.Contains(x, y) ||
                                   SettingWordPredictionRect.Contains(x, y) ||
                                   SettingLangViRect.Contains(x, y) ||
                                   SettingLangEnRect.Contains(x, y) ||
                                   SettingCheckUpdateBtnRect.Contains(x, y) ||
                                   SettingHelpBtnRect.Contains(x, y) ||
                                   SettingShortcutWriterRect.Contains(x, y) ||
                                   SettingShortcutCalcRect.Contains(x, y) ||
                                   SettingShortcutSlidesRect.Contains(x, y) ||
                                   SettingShortcutPdfRect.Contains(x, y) ||
                                   SettingAssocWriterRect.Contains(x, y) ||
                                   SettingAssocCalcRect.Contains(x, y) ||
                                   SettingAssocSlidesRect.Contains(x, y) ||
                                   SettingAssocPdfRect.Contains(x, y) ||
                                   SettingAssocSetAllBtnRect.Contains(x, y) ||
                                   SettingAssocUnsetAllBtnRect.Contains(x, y) ||
                                   SettingAssocWinSettingsRect.Contains(x, y));
                HoverRowIndex = hovSetting ? 200 : -1;
            }

            return (prevCard != HoverCardIndex || prevRow != HoverRowIndex || prevTab != HoverTabIndex ||
                    prevHelp != HoverHelpBtn || prevUpdate != HoverUpdateBtn || prevSideDoc != HoverSidebarDoc ||
                    prevSideRec != HoverSidebarRecent || prevSideStar != HoverSidebarStar ||
                    prevSideApp != HoverSidebarApp || prevPromo != HoverPromoBtn ||
                    prevStorage != HoverStorageManage || prevTmpl != HoverTemplateIndex || prevRowStar != HoverRowStar ||
                    prevUpdBtn != HoverRightPanelUpdateBtn);
        }

        public OfficeAppMode? HandleClick(int x, int y, int width, int height, out string docTitle)
        {
            string openFilePath;
            return HandleClick(x, y, width, height, out docTitle, out openFilePath);
        }

        public OfficeAppMode? HandleClick(int x, int y, int width, int height, out string docTitle, out string openFilePath)
        {
            docTitle = "";
            openFilePath = null;
            int currentScroll = ScrollY;

            // Search Box
            RectangleF searchBoxRect = GetSearchBoxBounds(width);
            if (searchBoxRect.Contains(x, y))
            {
                if (!string.IsNullOrEmpty(SearchKeyword) && x >= searchBoxRect.Right - 28f)
                {
                    SearchKeyword = "";
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

            // Help button
            RectangleF helpRect = GetHelpBtnBounds(width, height);
            if (helpRect.Contains(x, y))
            {
                ShowHelpDialog();
                return null;
            }

            // User avatar / Update button area
            RectangleF updateRect = GetUpdateBtnBounds(width, height);
            if (updateRect.Contains(x, y))
            {
                using (UpdateDialogForm dlg = new UpdateDialogForm(UpdateManager.LastReleaseInfo))
                {
                    dlg.ShowDialog();
                }
                return null;
            }

            // Creation cards (Chỉ có ở Trang chủ)
            if (ActiveTab == 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    RectangleF cb = GetCardBounds(i, width, height);
                    if (cb.Contains(x, y))
                    {
                        openFilePath = null;
                        if (i == 0) { docTitle = "Văn bản Mới.docx"; return OfficeAppMode.Writer; }
                        if (i == 1) { docTitle = "Bảng tính Mới.xlsx"; return OfficeAppMode.Calc; }
                        if (i == 2) { docTitle = "Trình chiếu Mới.pptx"; return OfficeAppMode.Slides; }
                        if (i == 3) { docTitle = "Tài liệu PDF Mới.pdf"; return OfficeAppMode.Pdf; }
                        if (i == 4) { docTitle = "Thiết kế Mới 1.svg"; return OfficeAppMode.Photo; }
                    }
                }
            }

            // Sidebar Tabs & Navigation Links (Trang chủ, Tài liệu của tôi, Thùng rác, Cài đặt)
            RectangleF myDocsTab = GetMyDocsTabBounds(width, height);
            RectangleF openDocNav = GetOpenDocNavBounds(width, height);
            RectangleF trashTab = GetTrashTabBounds(width, height);
            RectangleF settingsTab = GetSettingsTabBounds(width, height);

            if (myDocsTab.Contains(x, y))
            {
                ActiveTab = 0;
                SubFilter = 0;
                IndicatorTargetX = myDocsTab.X;
                IndicatorTargetW = myDocsTab.Width;
                ScrollY = 0;
                TargetScrollY = 0;
                return null;
            }
            if (openDocNav.Contains(x, y))
            {
                ActiveTab = 3;
                SubFilter = 0;
                IndicatorTargetX = openDocNav.X;
                IndicatorTargetW = openDocNav.Width;
                ScrollY = 0;
                TargetScrollY = 0;
                return null;
            }
            if (trashTab.Contains(x, y))
            {
                ActiveTab = 1;
                IndicatorTargetX = trashTab.X;
                IndicatorTargetW = trashTab.Width;
                ScrollY = 0;
                return null;
            }
            if (settingsTab.Contains(x, y))
            {
                ActiveTab = 2;
                IndicatorTargetX = settingsTab.X;
                IndicatorTargetW = settingsTab.Width;
                ScrollY = 0;
                return null;
            }

            // Sidebar App Shortcut Buttons (W / X / P / PDF) - 100% BLANK NEW DOCUMENTS
            if (GetAppNavBounds(0, width, height).Contains(x, y))
            {
                docTitle = "Văn bản Mới.docx";
                openFilePath = null;
                return OfficeAppMode.Writer;
            }
            if (GetAppNavBounds(1, width, height).Contains(x, y))
            {
                docTitle = "Bảng tính Mới.xlsx";
                openFilePath = null;
                return OfficeAppMode.Calc;
            }
            if (GetAppNavBounds(2, width, height).Contains(x, y))
            {
                docTitle = "Trình chiếu Mới.pptx";
                openFilePath = null;
                return OfficeAppMode.Slides;
            }
            if (GetAppNavBounds(3, width, height).Contains(x, y))
            {
                docTitle = "Tài liệu PDF Mới.pdf";
                openFilePath = null;
                return OfficeAppMode.Pdf;
            }
            if (GetAppNavBounds(4, width, height).Contains(x, y))
            {
                docTitle = "Thiết kế Mới 1.svg";
                openFilePath = null;
                return OfficeAppMode.Photo;
            }

            // Right Panel Clicks
            if (ActiveTab != 2)
            {
                // Promo Button -> Mở mẫu đơn xin nghỉ phép
                if (GetPromoBtnBounds(width).Contains(x, y))
                {
                    string tPath = EnsureTemplateLeaveRequest();
                    if (!string.IsNullOrEmpty(tPath))
                    {
                        AddRecentDocument(tPath, OfficeAppMode.Writer, "Mẫu - Đơn xin nghỉ phép.docx");
                        docTitle = "Mẫu - Đơn xin nghỉ phép.docx";
                        openFilePath = tPath;
                        return OfficeAppMode.Writer;
                    }
                }

                // Quản lý ổ đĩa -> Mở Explorer
                if (GetStorageManageBounds(width).Contains(x, y))
                {
                    try
                    {
                        string sysPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        string root = Path.GetPathRoot(sysPath);
                        System.Diagnostics.Process.Start("explorer.exe", !string.IsNullOrEmpty(root) ? root : "C:\\");
                    }
                    catch { }
                    return null;
                }

                // "Xem tất cả" bên cạnh "Mẫu nổi bật" -> Mở Tài liệu của tôi
                float pxAll = (width - RightPanelWidth) + 20f;
                float pwAll = RightPanelWidth - 40f;
                float tmplHeaderY = 266f;
                RectangleF viewAllTemplatesRect = new RectangleF(pxAll + pwAll - 65f, tmplHeaderY - 4f, 65f, 24f);
                if (viewAllTemplatesRect.Contains(x, y))
                {
                    ActiveTab = 3;
                    SubFilter = 0;
                    ScrollY = 0;
                    TargetScrollY = 0;
                    return null;
                }

                // 4 Mẫu nổi bật (Templates)
                if (GetTemplateCardBounds(0, width).Contains(x, y))
                {
                    string tPath = EnsureTemplateLeaveRequest();
                    if (!string.IsNullOrEmpty(tPath))
                    {
                        AddRecentDocument(tPath, OfficeAppMode.Writer, "Mẫu - Đơn xin nghỉ phép.docx");
                        docTitle = "Mẫu - Đơn xin nghỉ phép.docx";
                        openFilePath = tPath;
                        return OfficeAppMode.Writer;
                    }
                }
                if (GetTemplateCardBounds(1, width).Contains(x, y))
                {
                    string tPath = EnsureTemplateBusinessPlan();
                    if (!string.IsNullOrEmpty(tPath))
                    {
                        AddRecentDocument(tPath, OfficeAppMode.Slides, "Mẫu - Kế hoạch kinh doanh.pptx");
                        docTitle = "Mẫu - Kế hoạch kinh doanh.pptx";
                        openFilePath = tPath;
                        return OfficeAppMode.Slides;
                    }
                }
                if (GetTemplateCardBounds(2, width).Contains(x, y))
                {
                    string tPath = EnsureTemplateProjectReport();
                    if (!string.IsNullOrEmpty(tPath))
                    {
                        AddRecentDocument(tPath, OfficeAppMode.Writer, "Mẫu - Báo cáo tiến độ dự án.docx");
                        docTitle = "Mẫu - Báo cáo tiến độ dự án.docx";
                        openFilePath = tPath;
                        return OfficeAppMode.Writer;
                    }
                }
                if (GetTemplateCardBounds(3, width).Contains(x, y))
                {
                    string tPath = EnsureTemplateTimesheet();
                    if (!string.IsNullOrEmpty(tPath))
                    {
                        AddRecentDocument(tPath, OfficeAppMode.Calc, "Mẫu - Bảng chấm công.csv");
                        docTitle = "Mẫu - Bảng chấm công.csv";
                        openFilePath = tPath;
                        return OfficeAppMode.Calc;
                    }
                }

                // Nút "Cập nhật ngay" ở góc dưới bên phải (dưới Tự động lưu bản thảo an toàn)
                if (RightPanelUpdateBtnRect.Contains(x, y))
                {
                    using (UpdateDialogForm dlg = new UpdateDialogForm(UpdateManager.LastReleaseInfo))
                    {
                        dlg.ShowDialog();
                    }
                    return null;
                }
            }

            // Tab-specific actions
            if (ActiveTab == 3)
            {
                if (MyDocsOpenFileBtnRect.Contains(x, y))
                {
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Title = "Mở tài liệu văn phòng - ZENQ OFFICE";
                        ofd.Filter = "Tất cả tài liệu & Thiết kế (*.docx;*.xlsx;*.pptx;*.pdf;*.svg;*.png;*.jpg;*.jpeg)|*.docx;*.xlsx;*.pptx;*.pdf;*.svg;*.png;*.jpg;*.jpeg|Đồ họa Vector SVG (*.svg)|*.svg|Hình ảnh (*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.psd)|*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.psd|Word (*.docx)|*.docx|Excel (*.xlsx)|*.xlsx|PowerPoint (*.pptx)|*.pptx|PDF (*.pdf)|*.pdf|Tất cả tệp (*.*)|*.*";
                        ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            string chosenPath = ofd.FileName;
                            string ext = Path.GetExtension(chosenPath).ToLowerInvariant();
                            OfficeAppMode chosenMode = OfficeAppMode.Writer;
                            if (ext == ".xlsx" || ext == ".xls" || ext == ".csv") chosenMode = OfficeAppMode.Calc;
                            else if (ext == ".pptx" || ext == ".ppt") chosenMode = OfficeAppMode.Slides;
                            else if (ext == ".pdf") chosenMode = OfficeAppMode.Pdf;
                            else if (ext == ".svg" || ext == ".psd" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".tiff") chosenMode = OfficeAppMode.Photo;

                            AddRecentDocument(chosenPath, chosenMode);
                            docTitle = Path.GetFileName(chosenPath);
                            openFilePath = chosenPath;
                            return chosenMode;
                        }
                    }
                    return null;
                }

                for (int p = 0; p < MyDocsFilterPills.Count; p++)
                {
                    if (MyDocsFilterPills[p].Bounds.Contains(x, y))
                    {
                        SubFilter = MyDocsFilterPills[p].FilterId;
                        ScrollY = 0;
                        TargetScrollY = 0;
                        return null;
                    }
                }
            }
            else if (ActiveTab == 0)
            {
                float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;
                GetCenterContentMetrics(width, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);
                RectangleF viewAllRect = new RectangleF(startX + contentW - 120f, fileListY, 120f, 26f);
                if (viewAllRect.Contains(x, y))
                {
                    ActiveTab = 3;
                    SubFilter = 0;
                    ScrollY = 0;
                    TargetScrollY = 0;
                    return null;
                }
            }
            else if (ActiveTab == 1)
            {
                if (TrashDocs != null && TrashDocs.Count > 0)
                {
                    float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;
                    GetCenterContentMetrics(width, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);
                    RectangleF empBtn = new RectangleF(startX + contentW - 155f, fileListY - 2f, 155f, 30f);
                    if (empBtn.Contains(x, y))
                    {
                        var dr = MessageBox.Show("Bạn có chắc chắn muốn dọn sạch tất cả tệp trong Thùng rác không?", "Dọn sạch thùng rác", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (dr == DialogResult.Yes)
                        {
                            EmptyTrash();
                        }
                        return null;
                    }
                }
            }

            // File list clicks
            if (ActiveTab == 0 || ActiveTab == 1 || ActiveTab == 3)
            {
                List<RecentDocItem> currentList = GetFilteredDocs(ActiveTab);
                float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;
                GetCenterContentMetrics(width, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);
                float startY = listTopY - currentScroll;
                int rowHeight = 56;
                int rowTotalH = rowHeight + 2;

                int maxScroll = GetMaxScroll(height, width);
                if (x >= startX && x <= startX + contentW - (maxScroll > 0 ? 14f : 0f) && y >= listTopY && y <= height - 4f)
                {
                    float relY = y - startY;
                    if (relY >= 0)
                    {
                        int r = (int)(relY / rowTotalH);
                        float withinRowY = relY % rowTotalH;
                        if (r >= 0 && r < currentList.Count && withinRowY <= rowHeight)
                        {
                            float rowY = startY + r * rowTotalH;
                            RecentDocItem item = currentList[r];

                            if (ActiveTab == 0 || ActiveTab == 3)
                            {
                                // Click on Star icon -> toggle star
                                float dotX = startX + contentW - 24f;
                                if (x >= dotX - 44f && x <= dotX - 18f)
                                {
                                    item.IsStarred = !item.IsStarred;
                                    SaveRecentDocs();
                                    return null;
                                }

                                // Click on dots area -> move to trash
                                if (x >= dotX - 16f)
                                {
                                    RecentDocs.Remove(item);
                                    if (TrashDocs == null) TrashDocs = new List<RecentDocItem>();
                                    TrashDocs.Add(item);
                                    SaveRecentDocs();
                                    return null;
                                }

                                // Open document
                                docTitle = item.Title;
                                openFilePath = item.FilePath;
                                return item.Mode;
                            }
                            else
                            {
                                RectangleF resBtn = new RectangleF(startX + contentW - 135f, rowY + 14f, 65f, 28f);
                                if (resBtn.Contains(x, y))
                                {
                                    TrashDocs.Remove(item);
                                    if (RecentDocs == null) RecentDocs = new List<RecentDocItem>();
                                    RecentDocs.Insert(0, item);
                                    SaveRecentDocs();
                                    return null;
                                }
                                RectangleF purgeBtn = new RectangleF(startX + contentW - 60f, rowY + 14f, 50f, 28f);
                                if (purgeBtn.Contains(x, y))
                                {
                                    TrashDocs.Remove(item);
                                    SaveRecentDocs();
                                    return null;
                                }
                            }
                        }
                    }
                }
            }
            else if (ActiveTab == 2)
            {
                // Settings clicks
                if (SettingAiCopilotToggleRect.Contains(x, y)) { AppSettings.Instance.AiCopilotEnabled = !AppSettings.Instance.AiCopilotEnabled; AppSettings.Instance.Save(); return null; }
                if (SettingAiProviderGoogleRect.Contains(x, y)) { AppSettings.Instance.SelectedAiProvider = 0; AppSettings.Instance.Save(); return null; }
                if (SettingAiProviderOpenAiRect.Contains(x, y)) { AppSettings.Instance.SelectedAiProvider = 1; AppSettings.Instance.Save(); return null; }
                if (SettingAiProviderClaudeRect.Contains(x, y)) { AppSettings.Instance.SelectedAiProvider = 2; AppSettings.Instance.Save(); return null; }
                if (SettingAiProviderOllamaRect.Contains(x, y)) { AppSettings.Instance.SelectedAiProvider = 3; AppSettings.Instance.Save(); return null; }
                if (SettingAiConfigBtnRect.Contains(x, y)) { using (AiConfigDialog dlg = new AiConfigDialog()) { dlg.ShowDialog(); } return null; }
                if (SettingRibbonIslandRect.Contains(x, y)) { AppSettings.Instance.RibbonStyle = RibbonStyle.Island; AppSettings.Instance.Save(); return null; }
                if (SettingRibbonStandardRect.Contains(x, y)) { AppSettings.Instance.RibbonStyle = RibbonStyle.Standard; AppSettings.Instance.Save(); return null; }
                if (SettingThemeLightRect.Contains(x, y)) { AppSettings.Instance.Theme = AppTheme.Light; AppSettings.Instance.Save(); return null; }
                if (SettingThemeDarkRect.Contains(x, y)) { AppSettings.Instance.Theme = AppTheme.Dark; AppSettings.Instance.Save(); return null; }
                if (SettingAutoSaveToggleRect.Contains(x, y)) { AppSettings.Instance.AutoSaveEnabled = !AppSettings.Instance.AutoSaveEnabled; AppSettings.Instance.Save(); return null; }
                if (SettingAutoSave30Rect.Contains(x, y)) { AppSettings.Instance.AutoSaveIntervalSeconds = 30; AppSettings.Instance.Save(); return null; }
                if (SettingAutoSave60Rect.Contains(x, y)) { AppSettings.Instance.AutoSaveIntervalSeconds = 60; AppSettings.Instance.Save(); return null; }
                if (SettingAutoSave120Rect.Contains(x, y)) { AppSettings.Instance.AutoSaveIntervalSeconds = 120; AppSettings.Instance.Save(); return null; }
                if (SettingSmoothScrollRect.Contains(x, y)) { AppSettings.Instance.EnableSmoothScroll = !AppSettings.Instance.EnableSmoothScroll; AppSettings.Instance.Save(); return null; }
                if (SettingRulerRect.Contains(x, y)) { AppSettings.Instance.ShowRuler = !AppSettings.Instance.ShowRuler; AppSettings.Instance.Save(); return null; }
                if (SettingAutoCorrectViRect.Contains(x, y)) { AppSettings.Instance.AutoCorrectVietnamese = !AppSettings.Instance.AutoCorrectVietnamese; AppSettings.Instance.Save(); return null; }
                if (SettingWordPredictionRect.Contains(x, y)) { AppSettings.Instance.WordPredictionEnabled = !AppSettings.Instance.WordPredictionEnabled; AppSettings.Instance.Save(); return null; }
                if (SettingLangViRect.Contains(x, y)) { AppSettings.Instance.Language = AppLanguage.Vietnamese; AppSettings.Instance.Save(); return null; }
                if (SettingLangEnRect.Contains(x, y)) { AppSettings.Instance.Language = AppLanguage.English; AppSettings.Instance.Save(); return null; }
                if (SettingCheckUpdateBtnRect.Contains(x, y)) { using (UpdateDialogForm dlg = new UpdateDialogForm(UpdateManager.LastReleaseInfo)) { dlg.ShowDialog(); } return null; }
                if (SettingHelpBtnRect.Contains(x, y)) { ShowHelpDialog(); return null; }
                if (SettingShortcutWriterRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("ZenQ Writer", "-writer", "icon\\writer.ico", "Soạn thảo văn bản ZenQ Writer"); DesktopShortcutManager.DeleteShortcut("GenOffice Writer"); return null; }
                if (SettingShortcutCalcRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("ZenQ Calc", "-calc", "icon\\calc.ico", "Bảng tính ZenQ Calc"); DesktopShortcutManager.DeleteShortcut("GenOffice Calc"); return null; }
                if (SettingShortcutSlidesRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("ZenQ Slides", "-slides", "icon\\present.ico", "Trình chiếu ZenQ Slides"); DesktopShortcutManager.DeleteShortcut("GenOffice Slides"); return null; }
                if (SettingShortcutPdfRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("ZenQ PDF", "-pdf", "icon\\pdf.ico", "Đọc và chỉnh sửa ZenQ PDF"); DesktopShortcutManager.DeleteShortcut("GenOffice PDF"); return null; }
                if (SettingAssocWriterRect.Contains(x, y)) { FileAssociationManager.ToggleAppAssociation("-word"); return null; }
                if (SettingAssocCalcRect.Contains(x, y)) { FileAssociationManager.ToggleAppAssociation("-calc"); return null; }
                if (SettingAssocSlidesRect.Contains(x, y)) { FileAssociationManager.ToggleAppAssociation("-slides"); return null; }
                if (SettingAssocPdfRect.Contains(x, y)) { 
                    IntPtr hwnd = Form.ActiveForm != null ? Form.ActiveForm.Handle : IntPtr.Zero;
                    FileAssociationManager.ToggleAppAssociation("-pdf", hwnd); 
                    return null; 
                }
                if (SettingAssocSetAllBtnRect.Contains(x, y)) { 
                    FileAssociationManager.RegisterAllFileAssociations(); 
                    if (!FileAssociationManager.IsAppAssociated("-pdf"))
                    {
                        IntPtr hwnd = Form.ActiveForm != null ? Form.ActiveForm.Handle : IntPtr.Zero;
                        FileAssociationManager.PromptWindowsDefaultApp(".pdf", hwnd);
                        if (!FileAssociationManager.IsAppAssociated("-pdf"))
                        {
                            FileAssociationManager.OpenWindowsDefaultAppsSettings("ZenQOffice");
                        }
                    }
                    return null; 
                }
                if (SettingAssocUnsetAllBtnRect.Contains(x, y)) { FileAssociationManager.UnregisterAllFileAssociations(); return null; }
                if (SettingAssocWinSettingsRect.Contains(x, y)) { FileAssociationManager.OpenWindowsDefaultAppsSettings("ZenQOffice"); return null; }
            }

            return null;
        }
        #endregion


    }
}

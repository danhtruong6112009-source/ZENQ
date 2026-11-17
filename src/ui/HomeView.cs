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

        public bool IsTemplateDiscoveryMode { get; set; } = false;

        private TemplateDiscoveryView _templateDiscoveryView = new TemplateDiscoveryView();

        public TemplateDiscoveryView TemplateDiscovery => _templateDiscoveryView;

        private SharedFilesView _sharedFilesView = new SharedFilesView();

        public SharedFilesView SharedView => _sharedFilesView;



        public int LastMouseX = 0;

        public int LastMouseY = 0;



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

        public bool HoverSidebarToggle = false;

        public bool HoverPromoBtn = false;

        public bool HoverStorageManage = false;

        public int HoverTemplateIndex = -1;

        public int HoverRowStar = -1;

        public int HoverRowTrash = -1;

        public bool HoverMyDocsOpenFileBtn = false;

        public int HoverPillIndex = -1;

        public bool HoverRightPanelUpdateBtn = false;

        public RectangleF RightPanelUpdateBtnRect = RectangleF.Empty;

        public bool HoverRightPanelAuthBtn = false;

        public RectangleF RightPanelAuthBtnRect = RectangleF.Empty;

        public bool HoverRightPanelSignOutBtn = false;

        public RectangleF RightPanelSignOutBtnRect = RectangleF.Empty;

        public bool HoverRightPanelSharedBtn = false;

        public RectangleF RightPanelSharedBtnRect = RectangleF.Empty;



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
                if (IsTemplateDiscoveryMode)
                {
                    if (_templateDiscoveryView != null && _templateDiscoveryView.PreviewModalTemplate != null)
                    {
                        return _templateDiscoveryView.HoverModalCloseBtn ||
                               _templateDiscoveryView.HoverModalUseBtn ||
                               _templateDiscoveryView.HoverModalDeleteBtn ||
                               _templateDiscoveryView.HoverModalThumbLeftBtn ||
                               _templateDiscoveryView.HoverModalThumbRightBtn ||
                               _templateDiscoveryView.HoveredModalThumbIndex >= 0 ||
                               _templateDiscoveryView.HoveredModalRelatedIndex >= 0;
                    }

                    bool tmplHand = false;
                    if (_templateDiscoveryView != null)
                    {
                        tmplHand = _templateDiscoveryView.HoverBackButton || _templateDiscoveryView.HoverUploadButton || _templateDiscoveryView.HoverReloadButton || _templateDiscoveryView.HoverRetryOfflineButton ||
                                   _templateDiscoveryView.HoveredCardIndex >= 0 || _templateDiscoveryView.HoverTrendingLeftBtn ||
                                   _templateDiscoveryView.HoverTrendingRightBtn || _templateDiscoveryView.HoverForYouLeftBtn ||
                                   _templateDiscoveryView.HoverForYouRightBtn;
                    }
                    return tmplHand || HoverTabIndex >= 0 || HoverSidebarApp >= 0 || HoverSidebarToggle ||
                           HoverHelpBtn || HoverUpdateBtn || HoverPromoBtn || HoverRightPanelSharedBtn ||
                           HoverStorageManage || HoverRightPanelUpdateBtn || HoverRightPanelAuthBtn || HoverRightPanelSignOutBtn;
                }

                if (ActiveTab == 4 && _sharedFilesView.IsHoverHand) return true;

                return HoverTabIndex >= 0 || HoverCardIndex >= 0 || HoverRowIndex >= 0 ||

                       HoverHelpBtn || HoverUpdateBtn || HoverSidebarDoc || HoverSidebarRecent ||

                       HoverSidebarStar || HoverSidebarApp >= 0 || HoverSidebarToggle || HoverPromoBtn || HoverRightPanelSharedBtn ||

                       HoverStorageManage || HoverTemplateIndex >= 0 || HoverRowStar >= 0 || HoverRowTrash >= 0 ||

                       HoverMyDocsOpenFileBtn || HoverPillIndex >= 0 || HoverRightPanelUpdateBtn || HoverRightPanelAuthBtn || HoverRightPanelSignOutBtn;

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

        public int SettingsCategoryIndex = 0;

        public readonly RectangleF[] SettingsCategoryTabRects = new RectangleF[5];

        private float _settingsContentTotalH = 850f;

        private int _hoverSettingsControlId = -1;

        private readonly RectangleF[] _settingShortcutTileRects = new RectangleF[4];

        private readonly Dictionary<string, float> _settingTogglePos = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _settingToggleTarget = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _settingHoverPos = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _settingHoverTarget = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _settingTrayCurX = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _settingTrayCurW = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _settingTrayTargetX = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _settingTrayTargetW = new Dictionary<string, float>();
        private float _catPillCurX = -1f, _catPillCurW = -1f, _catPillTargetX = -1f, _catPillTargetW = -1f;
        private float _aiModelCurX = -1f, _aiModelCurY = -1f, _aiModelCurW = -1f, _aiModelTargetX = -1f, _aiModelTargetY = -1f, _aiModelTargetW = -1f;
        private string _settingPulseKey = null;
        private float _settingPulseVal = 0f;
        private readonly List<string> _animKeyScratch = new List<string>(32);



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



        private long _homeScrollLastTs = 0;

        public bool UpdateSmoothScroll(int windowHeight = 0, int windowWidth = 0)

        {
            double dt = FluentMotion.DeltaSeconds(ref _homeScrollLastTs);
            float scrollBlend = FluentMotion.DampFactor(dt, 0.028);

            int h = (windowHeight > 200) ? windowHeight : _lastHeight;

            int w = (windowWidth > 200) ? windowWidth : _lastWidth;

            int maxScroll = GetMaxScroll(h, w);

            TargetScrollY = Math.Max(0f, Math.Min(maxScroll, TargetScrollY));

            bool isAnimating = false;



            if (_templateDiscoveryView != null && IsTemplateDiscoveryMode)

            {

                if (_templateDiscoveryView.UpdateAnimation()) isAnimating = true;

            }



            float smooth = SmoothScrollY;
            if (FluentMotion.StepAxis(ref smooth, TargetScrollY, scrollBlend, 0.35f))
            {
                SmoothScrollY = smooth;
                isAnimating = true;
            }
            float sbW = _sidebarAnimWidth;
            if (FluentMotion.StepAxis(ref sbW, _sidebarTargetWidth, FluentMotion.DampFactor(dt, 0.062), 0.35f))
            {
                _sidebarAnimWidth = sbW;
                isAnimating = true;
            }
            if (!isAnimating && SmoothScrollY == TargetScrollY && _sidebarAnimWidth == _sidebarTargetWidth)
            {
                _homeScrollLastTs = 0;
            }



            // Animate Sliding Indicator Underline

            if (IndicatorCurrentX > 0 && IndicatorTargetX > 0)

            {

                if (Math.Abs(IndicatorTargetX - IndicatorCurrentX) > 0.3f)

                {

                    IndicatorCurrentX += (IndicatorTargetX - IndicatorCurrentX) * FluentMotion.DampFactor(dt, 0.040);

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

            if (ActiveTab == 2)
            {
                if (StepSettingsAnimations(dt)) isAnimating = true;
            }

            if (!isAnimating && SmoothScrollY == TargetScrollY && _sidebarAnimWidth == _sidebarTargetWidth)
            {
                _homeScrollLastTs = 0;
            }

            return isAnimating;

        }

        private bool StepAnimMap(Dictionary<string, float> cur, Dictionary<string, float> target, float blend, float snapEps)
        {
            if (cur.Count == 0) return false;
            bool changed = false;
            _animKeyScratch.Clear();
            foreach (var k in cur.Keys) _animKeyScratch.Add(k);
            for (int i = 0; i < _animKeyScratch.Count; i++)
            {
                string key = _animKeyScratch[i];
                if (!target.TryGetValue(key, out float tgt)) continue;
                float val = cur[key];
                if (FluentMotion.StepAxis(ref val, tgt, blend, snapEps))
                {
                    cur[key] = val;
                    changed = true;
                }
            }
            return changed;
        }

        private bool StepSettingsAnimations(double dt)
        {
            bool changed = false;
            float toggleBlend = FluentMotion.DampFactor(dt, 0.042);
            float hoverBlend = FluentMotion.DampFactor(dt, 0.055);
            float pillBlend = FluentMotion.DampFactor(dt, 0.040);

            if (StepAnimMap(_settingTogglePos, _settingToggleTarget, toggleBlend, 0.004f)) changed = true;
            if (StepAnimMap(_settingHoverPos, _settingHoverTarget, hoverBlend, 0.005f)) changed = true;

            if (_catPillCurX >= 0f && _catPillTargetX >= 0f)
            {
                if (FluentMotion.StepAxis(ref _catPillCurX, _catPillTargetX, pillBlend, 0.35f)) changed = true;
                if (FluentMotion.StepAxis(ref _catPillCurW, _catPillTargetW, pillBlend, 0.35f)) changed = true;
            }
            if (_aiModelCurX >= 0f && _aiModelTargetX >= 0f)
            {
                if (FluentMotion.StepAxis(ref _aiModelCurX, _aiModelTargetX, pillBlend, 0.35f)) changed = true;
                if (FluentMotion.StepAxis(ref _aiModelCurY, _aiModelTargetY, pillBlend, 0.35f)) changed = true;
                if (FluentMotion.StepAxis(ref _aiModelCurW, _aiModelTargetW, pillBlend, 0.35f)) changed = true;
            }
            if (StepAnimMap(_settingTrayCurX, _settingTrayTargetX, pillBlend, 0.35f)) changed = true;
            if (StepAnimMap(_settingTrayCurW, _settingTrayTargetW, pillBlend, 0.35f)) changed = true;

            if (_settingPulseVal > 0f)
            {
                float pv = _settingPulseVal;
                if (FluentMotion.StepAxis(ref pv, 0f, FluentMotion.DampFactor(dt, 0.065), 0.01f))
                {
                    _settingPulseVal = pv;
                    changed = true;
                }
                else
                {
                    _settingPulseVal = 0f;
                    _settingPulseKey = null;
                }
            }
            return changed;
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

                return (int)Math.Ceiling(GetSettingsContentHeight(width));

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

                int totalH = (int)Math.Ceiling(GetSettingsContentHeight(w));

                int viewportH = Math.Max(100, h - 124);

                return Math.Max(0, totalH - viewportH);

            }

            else if (ActiveTab == 4)

            {

                int totalH = _sharedFilesView.GetContentHeight(SearchKeyword);

                int viewportH = Math.Max(120, h - 76);

                return Math.Max(0, totalH - viewportH);

            }

            else

            {

                List<RecentDocItem> currentList = GetFilteredDocs(ActiveTab);

                float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;

                GetCenterContentMetrics(w, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);

                if (ActiveTab == 0 && !IsTemplateDiscoveryMode)

                {

                    int rowsH = (currentList != null && currentList.Count > 0) ? (currentList.Count * 58 + 36) : 90;

                    int docBottom = (int)Math.Ceiling(listTopY) + rowsH;

                    return Math.Max(0, docBottom - (h - 4));

                }

                if (currentList == null || currentList.Count == 0) return 0;

                int totalListHeight = currentList.Count * 58 + 96;

                int viewportH = Math.Max(100, h - (int)listTopY - 4);

                return Math.Max(0, totalListHeight - viewportH);

            }

        }



        public bool HandleScrollBarMouseDown(int mouseX, int mouseY, int width, int height)

        {

            if (ActiveTab == 0 && IsTemplateDiscoveryMode) return false;

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

            if (ActiveTab == 0 && IsTemplateDiscoveryMode) return false;

            if (IsDraggingScrollBar)

            {

                float barH = Math.Max(1f, ScrollBarBounds.Height - 8f);

                float thumbH = Math.Max(20f, ScrollBarThumbBounds.Height);

                float trackScrollH = Math.Max(1f, barH - thumbH);

                int deltaMouseY = mouseY - _scrollBarDragStartMouseY;

                int maxScroll = GetMaxScroll(height, width);

                float deltaScroll = ((float)deltaMouseY / trackScrollH) * maxScroll;

                TargetScrollY = Math.Max(0f, Math.Min(maxScroll, _scrollBarDragStartScrollY + deltaScroll));

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

            if (_templateDiscoveryView != null)

            {

                _templateDiscoveryView.HandleMouseUp();

            }

        }



        public const float ExpandedSidebarWidth = 220f;

        public const float CollapsedSidebarWidth = 52f;

        public bool IsSidebarCollapsed { get; private set; } = false;

        private float _sidebarAnimWidth = ExpandedSidebarWidth;

        private float _sidebarTargetWidth = ExpandedSidebarWidth;

        public float SidebarWidth => _sidebarAnimWidth;

        public float SidebarExpandFactor => Math.Clamp((_sidebarAnimWidth - CollapsedSidebarWidth) / (ExpandedSidebarWidth - CollapsedSidebarWidth), 0f, 1f);

        public void ToggleSidebarCollapsed() => SetSidebarCollapsed(!IsSidebarCollapsed);

        public void SetSidebarCollapsed(bool collapsed)
        {
            IsSidebarCollapsed = collapsed;
            _sidebarTargetWidth = collapsed ? CollapsedSidebarWidth : ExpandedSidebarWidth;
            _homeScrollLastTs = System.Diagnostics.Stopwatch.GetTimestamp();
        }

        public RectangleF GetSidebarToggleBounds(int width = 0, int height = 0)
        {
            float t = SidebarExpandFactor;
            float collapsedX = (CollapsedSidebarWidth - 26f) / 2f;
            float expandedX = 16f;
            float tx = collapsedX + (expandedX - collapsedX) * t;
            return new RectangleF(tx, 37f, 26f, 26f);
        }

        public const float ExpandedRightPanelWidth = 280f;

        public const float CollapsedRightPanelWidth = 64f;

        public float RightPanelWidth => CollapsedRightPanelWidth + (ExpandedRightPanelWidth - CollapsedRightPanelWidth) * SidebarExpandFactor;

        private RectangleF _compactRightAvatarRect = RectangleF.Empty;

        private bool _hoverRightCompactAvatar = false;

        private long _cachedDriveTicks = 0;

        private string _cachedDriveLetter = "C:";

        private double _cachedUsedGb = 0;

        private double _cachedTotalGb = 100.0;

        private int _cachedUsedPct = 35;

        private void GetCachedDriveUsage(out string driveLetter, out double usedGb, out double totalGb, out int usedPct)
        {
            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            if (_cachedDriveTicks == 0 || (now - _cachedDriveTicks) > System.Diagnostics.Stopwatch.Frequency * 5)
            {
                _cachedDriveTicks = now;
                long totalBytes = 0;
                long freeBytes = 0;
                string dl = "C:";
                try
                {
                    string sysPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string root = Path.GetPathRoot(sysPath);
                    if (!string.IsNullOrEmpty(root))
                    {
                        DriveInfo drive = new DriveInfo(root);
                        dl = drive.Name.TrimEnd('\\');
                        totalBytes = drive.TotalSize;
                        freeBytes = drive.AvailableFreeSpace;
                    }
                }
                catch
                {
                    try
                    {
                        DriveInfo d = new DriveInfo("C");
                        dl = "C:";
                        totalBytes = d.TotalSize;
                        freeBytes = d.AvailableFreeSpace;
                    }
                    catch { }
                }
                long usedBytes = Math.Max(0, totalBytes - freeBytes);
                _cachedDriveLetter = dl;
                _cachedUsedGb = Math.Round(usedBytes / (1024.0 * 1024 * 1024), 1);
                _cachedTotalGb = (totalBytes > 0) ? Math.Round(totalBytes / (1024.0 * 1024 * 1024), 1) : 100.0;
                _cachedUsedPct = (totalBytes > 0) ? (int)Math.Max(1, Math.Min(99, Math.Round((double)usedBytes / (double)totalBytes * 100.0))) : 35;
            }
            driveLetter = _cachedDriveLetter;
            usedGb = _cachedUsedGb;
            totalGb = _cachedTotalGb;
            usedPct = _cachedUsedPct;
        }

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

            float searchW = Math.Min(480f, rightEdge - contentLeft - 140f);

            if (searchW < 220f) searchW = 220f;

            return new RectangleF(contentLeft, 16f, searchW, 38f);

        }



        public RectangleF GetHelpBtnBounds(int width, int scrollY = 0)

        {

            float btnSize = 36f;

            if (ActiveTab == 2)

            {

                float px, pw, br;

                GetSettingsLayoutMetrics(width, out px, out pw, out br);

                return new RectangleF(px + pw - btnSize, 20f, btnSize, btnSize);

            }

            return new RectangleF(width - RightPanelWidth - 24f - btnSize - 8f - btnSize, 17f, btnSize, btnSize);

        }



        public RectangleF GetUpdateBtnBounds(int width, int scrollY = 0)

        {

            return RectangleF.Empty;

        }



        public RectangleF GetFeaturesTabBounds(int width, int scrollY = 0)

        {

            return GetCardBounds(0, width, _lastHeight);

        }



        public RectangleF GetMyDocsTabBounds(int width, int scrollY = 0)
        {
            return new RectangleF(10f, 70f, SidebarWidth - 20f, 34f);
        }

        public RectangleF GetTemplateDiscoveryNavBounds(int width, int scrollY = 0)
        {
            return new RectangleF(10f, 108f, SidebarWidth - 20f, 34f);
        }

        public RectangleF GetOpenDocNavBounds(int width, int scrollY = 0)
        {
            return new RectangleF(10f, 146f, SidebarWidth - 20f, 34f);
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
            return new RectangleF(10f, 184f, SidebarWidth - 20f, 34f);
        }

        public RectangleF GetAppNavBounds(int appIndex, int width, int scrollY = 0)
        {
            float appNavY = 250f + appIndex * 34f;
            return new RectangleF(10f, appNavY, SidebarWidth - 20f, 30f);
        }



        public RectangleF GetPromoBtnBounds(int width)

        {

            if (IsSidebarCollapsed || SidebarExpandFactor < 0.5f)

            {

                float cx = width - RightPanelWidth / 2f;

                return new RectangleF(cx - 21f, 70f, 42f, 42f);

            }

            float panelX = width - RightPanelWidth;

            float px = panelX + 20f;

            return new RectangleF(px + 14f, 150f, 130f, 28f);

        }

        public RectangleF GetRightSharedBounds(int width)
        {
            if (IsSidebarCollapsed || SidebarExpandFactor < 0.5f)
            {
                float cx = width - RightPanelWidth / 2f;
                return new RectangleF(cx - 24f, 120f, 48f, 64f);
            }
            float panelX = width - RightPanelWidth;
            return new RectangleF(panelX + 20f, 198f, ExpandedRightPanelWidth - 40f, 64f);
        }

        public RectangleF GetStorageManageBounds(int width)

        {

            if (IsSidebarCollapsed || SidebarExpandFactor < 0.5f)

            {

                float cx = width - RightPanelWidth / 2f;

                return new RectangleF(cx - 24f, 192f, 48f, 64f);

            }

            float panelX = width - RightPanelWidth;

            float px = panelX + 20f;

            float pw = RightPanelWidth - 40f;

            return new RectangleF(px + pw - 48f, 276f, 48f, 20f);

        }







        public RectangleF GetBugReportTabBounds(int width, int scrollY = 0)
        {
            float h = (scrollY > 200) ? scrollY : _lastHeight;
            if (h < 200) h = 750f;
            return new RectangleF(10f, h - 96f, SidebarWidth - 20f, 38f);
        }

        public RectangleF GetSettingsTabBounds(int width, int scrollY = 0)
        {
            float h = (scrollY > 200) ? scrollY : _lastHeight;
            if (h < 200) h = 750f;
            return new RectangleF(10f, h - 52f, SidebarWidth - 20f, 38f);
        }



        public RectangleF GetAiTabBounds(int width, int scrollY = 0)

        {

            return GetSettingsTabBounds(width, scrollY);

        }



        public void GetCenterContentMetrics(int width, out float startX, out float contentW, out float bannerH, out float cardY, out float fileListY, out float colHeaderY, out float listTopY)

        {

            startX = SidebarWidth + 24f;

            contentW = (ActiveTab == 2)
                ? Math.Max(460f, width - SidebarWidth - 48f)
                : Math.Max(400f, width - SidebarWidth - RightPanelWidth - 48f);

            float viewHRef = (_lastHeight > 200) ? _lastHeight : 760f;

            float rawBannerH = (float)Math.Round(contentW * 3456.0 / 8064.0);

            float maxBannerH = Math.Max(180f, Math.Min(300f, viewHRef * 0.36f));

            bannerH = (ActiveTab == 0) ? Math.Min(rawBannerH, maxBannerH) : 0f;

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

            float effCardY = (ActiveTab == 0 && !IsTemplateDiscoveryMode) ? (cardY - SmoothScrollY) : cardY;

            return new RectangleF(startX + index * (cardW + cardGap), effCardY, cardW, 84f);

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

            _templateDiscoveryView.RecentDocsProvider = () => RecentDocs;

            DatabaseAuthService.Initialize();

            DatabaseAuthService.OnAuthStateChanged += () =>

            {

                try { AppForm.Instance?.Invalidate(); } catch { }

            };



            try

            {

                DesktopShortcutManager.CleanObsoleteShortcuts();

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



                            // Loại bỏ triệt để các tệp mẫu ảo, chỉ giữ tệp thật tồn tại trên máy

                            bool isFake = fpath.Contains("Templates\\Don_Xin") || fpath.Contains("Templates\\Bang_Cham") || fpath.Contains("Templates\\Bao_Cao") || fpath.Contains("Templates\\Ke_Hoach") || fpath.Contains("Templates\\Mau_") || !File.Exists(fpath);

                            if (!isFake)

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



            if (RecentDocs.Count == 0)

            {

                ScanDeviceForDocuments();

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

                    g.FillRectangle(barB, rect.X + 1f, rect.Y + 7f, 3.2f, rect.Height - 14f);

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



            float expandT = SidebarExpandFactor;

            float expandedIconX = rect.X + 14f;

            float collapsedIconX = 10f + ((CollapsedSidebarWidth - 20f) - 18f) / 2f;

            float iconX = collapsedIconX + (expandedIconX - collapsedIconX) * expandT;

            Color iconCol = isActive ? Color.FromArgb(37, 99, 235) : (isHover ? Color.FromArgb(15, 23, 42) : Color.FromArgb(100, 116, 139));

            if (drawIcon != null)

            {

                drawIcon(g, iconX, rect.Y + (rect.Height - 18f) / 2f, 18f, iconCol);

            }



            if (expandT > 0.35f)

            {

                int textAlpha = (int)Math.Round(255f * Math.Clamp((expandT - 0.35f) / 0.65f, 0f, 1f));

                Color baseTextCol = isActive ? Color.FromArgb(15, 23, 42) : (isHover ? Color.FromArgb(15, 23, 42) : Color.FromArgb(71, 85, 105));

                using (Font f = new Font("Segoe UI", 9.8f, isActive ? FontStyle.Bold : FontStyle.Regular))

                using (SolidBrush tb = new SolidBrush(Color.FromArgb(textAlpha, baseTextCol)))

                {

                    g.DrawString(text, f, tb, rect.X + 40f, rect.Y + (rect.Height - f.Height) / 2f);

                }

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

            string supText = AppL10n.T("Thử nghiệm", "Beta");



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

                string bText = AppL10n.T("THỬ NGHIỆM", "BETA");

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



        private void DrawVectorFeedbackIcon(Graphics g, float cx, float cy, float size, Color color)

        {

            FluentIconRenderer.DrawGlyph(g, '\uED15', new RectangleF(cx - size / 2f, cy - size / 2f, size, size), color, size * 0.72f);

        }



        private void DrawVectorGearIcon(Graphics g, float cx, float cy, float size, Color color)

        {

            FluentIconRenderer.DrawGlyph(g, '\uE713', new RectangleF(cx - size / 2f, cy - size / 2f, size, size), color, size * 0.72f);

        }



        private void ShowHelpDialog()

        {

            using (Form dlg = new Form())

            {

                dlg.Text = AppL10n.T("Giới thiệu & Hướng dẫn - ZENQ OFFICE", "About & Guide - ZENQ OFFICE");

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

                    Text = AppL10n.T("Phát triển bởi NEXTGENEDUSPACE STUDIO • Tác giả: Trương Nguyễn Thế Danh © 2026", "Developed by NEXTGENEDUSPACE STUDIO • Author: Trương Nguyễn Thế Danh © 2026"),

                    Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),

                    ForeColor = Color.FromArgb(37, 99, 235),

                    AutoSize = true,

                    Location = new Point(20, 52),

                    UseMnemonic = false

                };

                pnl.Controls.Add(lblSub);



                Label lblDesc = new Label

                {

                    Text = AppL10n.T(
                           "Bộ ứng dụng văn phòng đa năng hiện đại tích hợp đầy đủ:\n" +
                           "• Writer: Soạn thảo văn bản chuyên nghiệp (.docx)\n" +
                           "• Calc: Bảng tính số liệu thông minh (.xlsx, .csv)\n" +
                           "• Slides: Trình chiếu & Thuyết trình chuyên nghiệp (.pptx)\n" +
                           "• PDF: Đọc, chú thích, ký & chỉnh sửa tài liệu PDF\n\n" +
                           "Phím tắt nhanh:\n" +
                           "• Ctrl + N: Tạo tài liệu mới          • Ctrl + O: Mở tài liệu có sẵn\n" +
                           "• Ctrl + S: Lưu tài liệu hiện tại        • Ctrl + F: Tìm kiếm & Thay thế\n" +
                           "• F5: Bắt đầu trình chiếu Slide",
                           "Modern all-in-one productivity suite:\n" +
                           "• Writer: Professional word processing (.docx)\n" +
                           "• Calc: Smart spreadsheets & formulas (.xlsx, .csv)\n" +
                           "• Slides: Presentations & animations (.pptx)\n" +
                           "• PDF: Read, annotate, sign & edit PDFs\n\n" +
                           "Quick shortcuts:\n" +
                           "• Ctrl + N: New document          • Ctrl + O: Open existing file\n" +
                           "• Ctrl + S: Save current file      • Ctrl + F: Find & Replace\n" +
                           "• F5: Start Slide Show"),

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

                    Text = AppL10n.T("• Lưu ý về tính tương thích định dạng:", "• Format Compatibility Note:"),

                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),

                    ForeColor = Color.FromArgb(180, 83, 9),

                    AutoSize = true,

                    Location = new Point(14, 10),

                    UseMnemonic = false

                };

                pnlNote.Controls.Add(lblNoteTitle);



                Label lblNoteContent = new Label

                {

                    Text = AppL10n.T(
                        "ZENQ OFFICE luôn nỗ lực tối đa để hiển thị và xử lý tài liệu chuẩn xác nhất. Tuy nhiên, do sự khác biệt về đặc tả kỹ thuật và tiêu chuẩn phần mềm giữa các nền tảng, chúng tôi không thể cam kết rằng mọi định dạng nâng cao sẽ hiển thị giống hệt 100% như trên các bộ ứng dụng Office khác.",
                        "ZENQ OFFICE strives to render and process documents as accurately as possible. However, due to specification differences across software platforms, we cannot guarantee 100% identical rendering for every advanced formatting feature compared to other Office suites."),

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

                    Text = AppL10n.T("Kiểm tra cập nhật", "Check Updates"),

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

                    Text = AppL10n.T("Đóng", "Close"),

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

            float expandT = SidebarExpandFactor;

            float iconSize = 24f;

            float expandedIconX = rect.X + 12f;

            float collapsedIconX = 10f + ((CollapsedSidebarWidth - 20f) - iconSize) / 2f;

            float iconX = collapsedIconX + (expandedIconX - collapsedIconX) * expandT;

            float iconY = rect.Y + (rect.Height - iconSize) / 2f;

            if (!AppIconRenderer.DrawAppIcon(g, iconX, iconY, iconSize, mode))

            {

                DrawVectorAppEmblem(g, iconX, iconY, iconSize, mode);

            }



            if (expandT > 0.35f)

            {

                int textAlpha = (int)Math.Round(255f * Math.Clamp((expandT - 0.35f) / 0.65f, 0f, 1f));

                Color baseTextCol = isHover ? Color.FromArgb(15, 23, 42) : Color.FromArgb(71, 85, 105);

                using (Font tf = new Font("Segoe UI", 9f, isHover ? FontStyle.Bold : FontStyle.Regular))

                using (SolidBrush tb = new SolidBrush(Color.FromArgb(textAlpha, baseTextCol)))

                {

                    g.DrawString(text, tf, tb, rect.X + 44f, rect.Y + (rect.Height - tf.Height) / 2f);

                }

            }

        }

        public void DrawSidebarToggleButton(Graphics g, RectangleF rect, bool isHover)
        {
            if (isHover)
            {
                using (GraphicsPath hp = CreateRoundedRectanglePath(rect, 6f))
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(238, 242, 247)))
                {
                    g.FillPath(hb, hp);
                }
            }

            float iw = 16f;
            float ih = 13f;
            float ix = (float)Math.Round(rect.X + (rect.Width - iw) / 2f);
            float iy = (float)Math.Round(rect.Y + (rect.Height - ih) / 2f);
            RectangleF box = new RectangleF(ix, iy, iw, ih);
            Color fg = isHover ? Color.FromArgb(15, 23, 42) : Color.FromArgb(68, 75, 89);

            using (GraphicsPath boxPath = CreateRoundedRectanglePath(box, 2.5f))
            using (SolidBrush paneBrush = new SolidBrush(fg))
            using (Pen boxPen = new Pen(fg, 1.5f) { LineJoin = LineJoin.Round })
            {
                var st = g.Save();
                g.SetClip(new RectangleF(ix, iy, 5.5f, ih), CombineMode.Intersect);
                g.FillPath(paneBrush, boxPath);
                g.Restore(st);

                g.DrawPath(boxPen, boxPath);
                g.DrawLine(boxPen, ix + 5.5f, iy + 0.5f, ix + 5.5f, iy + ih - 0.5f);
            }
        }

        private void DrawCollapsedSidebarTooltip(Graphics g, int width, int height)
        {
            string tip = null;
            RectangleF anchor = RectangleF.Empty;
            bool anchorRightPanel = false;

            if (HoverSidebarToggle)
            {
                tip = IsSidebarCollapsed
                    ? AppL10n.T("Mở rộng thanh bên", "Expand sidebar")
                    : AppL10n.T("Thu gọn thanh bên", "Collapse sidebar");
                anchor = GetSidebarToggleBounds(width, height);
            }
            else if (IsSidebarCollapsed || SidebarExpandFactor < 0.25f)
            {
                if (HoverTabIndex == 0) { tip = AppL10n.T("Trang chủ", "Home"); anchor = GetMyDocsTabBounds(width, height); }
                else if (HoverTabIndex == 10) { tip = AppL10n.T("Khám phá mẫu", "Templates"); anchor = GetTemplateDiscoveryNavBounds(width, height); }
                else if (HoverTabIndex == 3 || HoverSidebarDoc) { tip = AppL10n.T("Tài liệu của tôi", "My Documents"); anchor = GetOpenDocNavBounds(width, height); }
                else if (HoverTabIndex == 1) { tip = AppL10n.T("Thùng rác", "Trash"); anchor = GetTrashTabBounds(width, height); }
                else if (HoverTabIndex == 4) { tip = AppL10n.T("Báo lỗi & Góp ý", "Feedback"); anchor = GetBugReportTabBounds(width, height); }
                else if (HoverTabIndex == 2) { tip = AppL10n.T("Cài đặt", "Settings"); anchor = GetSettingsTabBounds(width, height); }
                else if (HoverSidebarApp >= 0 && HoverSidebarApp < 5)
                {
                    string[] appNames = { AppL10n.T("Tài liệu", "Document"), AppL10n.T("Bảng tính", "Spreadsheet"), AppL10n.T("Thuyết trình", "Presentation"), "PDF", AppL10n.T("Đồ họa & Ảnh", "Design & Photo") };
                    tip = appNames[HoverSidebarApp];
                    anchor = GetAppNavBounds(HoverSidebarApp, width, height);
                }
                else if (ActiveTab != 2)
                {
                    if (HoverPromoBtn)
                    {
                        bool showBackToHome = (ActiveTab == 0 && IsTemplateDiscoveryMode);
                        tip = showBackToHome ? AppL10n.T("Về Trang chủ", "Back Home") : AppL10n.T("Khám phá mẫu", "Templates");
                        anchor = GetPromoBtnBounds(width);
                        anchorRightPanel = true;
                    }
                    else if (HoverStorageManage)
                    {
                        GetCachedDriveUsage(out string dl, out double uGb, out double tGb, out int uPct);
                        tip = string.Format(AppL10n.T("Ổ đĩa {0} • {1:0.#}/{2:0.#} GB ({3}%) • Quản lý", "Drive {0} • {1:0.#}/{2:0.#} GB ({3}%) • Manage"), dl, uGb, tGb, uPct);
                        anchor = GetStorageManageBounds(width);
                        anchorRightPanel = true;
                    }
                    else if (HoverRightPanelSharedBtn && !RightPanelSharedBtnRect.IsEmpty)
                    {
                        long cu = DatabaseAuthService.IsLoggedIn ? CloudShareService.SenderUsedBytes : 0L;
                        int cp = DatabaseAuthService.IsLoggedIn ? (int)Math.Round(100.0 * cu / CloudShareService.CloudQuotaBytes) : 0;
                        tip = DatabaseAuthService.IsLoggedIn
                            ? string.Format(AppL10n.T("Được chia sẻ ({0}% · {1} / 200 MB)", "Shared ({0}% · {1} / 200 MB)"), cp, CloudShareService.FormatSize(cu))
                            : AppL10n.T("Được chia sẻ", "Shared Files");
                        anchor = RightPanelSharedBtnRect;
                        anchorRightPanel = true;
                    }
                    else if (HoverRightPanelAuthBtn && !RightPanelAuthBtnRect.IsEmpty)
                    {
                        tip = AppL10n.T("Đăng nhập / Đăng ký", "Sign In / Sign Up");
                        anchor = RightPanelAuthBtnRect;
                        anchorRightPanel = true;
                    }
                    else if (HoverRightPanelSignOutBtn && !RightPanelSignOutBtnRect.IsEmpty)
                    {
                        tip = AppL10n.T("Đăng xuất", "Sign Out");
                        anchor = RightPanelSignOutBtnRect;
                        anchorRightPanel = true;
                    }
                    else if (_hoverRightCompactAvatar && !_compactRightAvatarRect.IsEmpty && DatabaseAuthService.IsLoggedIn && DatabaseAuthService.CurrentUser != null)
                    {
                        var cu = DatabaseAuthService.CurrentUser;
                        string dn = !string.IsNullOrWhiteSpace(cu.FullName) ? cu.FullName : cu.Username;
                        tip = string.Format("{0} (@{1}) • {2}", dn, cu.Username, AppL10n.TrRole(cu.RoleDisplayName));
                        anchor = _compactRightAvatarRect;
                        anchorRightPanel = true;
                    }
                    else if (HoverRightPanelUpdateBtn && !RightPanelUpdateBtnRect.IsEmpty)
                    {
                        tip = AppL10n.T("Cập nhật ngay", "Update Now");
                        anchor = RightPanelUpdateBtnRect;
                        anchorRightPanel = true;
                    }
                }
            }

            if (string.IsNullOrEmpty(tip) || anchor.IsEmpty) return;

            using (Font tf = new Font("Segoe UI", 8.8f, FontStyle.Regular))
            {
                SizeF tsz = g.MeasureString(tip, tf);
                float tw = tsz.Width + 16f;
                float th = 26f;
                float tx = anchorRightPanel ? Math.Max(8f, (width - RightPanelWidth) - tw - 6f) : (SidebarWidth + 6f);
                float ty = anchor.Y + (anchor.Height - th) / 2f;
                if (ty < 6f) ty = 6f;
                if (ty + th > height - 6f) ty = height - th - 6f;

                RectangleF tipRect = new RectangleF(tx, ty, tw, th);
                using (GraphicsPath tp = CreateRoundedRectanglePath(tipRect, 6f))
                using (SolidBrush sh = new SolidBrush(Color.FromArgb(24, 15, 23, 42)))
                using (SolidBrush bg = new SolidBrush(Color.White))
                using (Pen bd = new Pen(Color.FromArgb(220, 226, 234), 1f))
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
                {
                    RectangleF shadowRect = new RectangleF(tx + 1f, ty + 2f, tw, th);
                    using (GraphicsPath sp = CreateRoundedRectanglePath(shadowRect, 6f)) g.FillPath(sh, sp);
                    g.FillPath(bg, tp);
                    g.DrawPath(bd, tp);
                    g.DrawString(tip, tf, tb, tx + 8f, ty + (th - tsz.Height) / 2f);
                }
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



        private void DrawRightSidebarPanel(Graphics g, int width, int height)

        {

            float panelX = width - RightPanelWidth;

            float panelW = RightPanelWidth;

            float t = SidebarExpandFactor;

            float cx = panelX + panelW / 2f;



            // Smooth sequential cross-fade + continuous geometry morph (zero mid-flight text/ring collision)
            float textOutF = FluentMotion.Clamp01((t - 0.52f) / 0.43f);
            float compactInF = FluentMotion.Clamp01((0.48f - t) / 0.43f);

            int aOut = (int)Math.Round(255f * textOutF);

            int aIn = (int)Math.Round(255f * compactInF);



            // Panel background + left border

            using (SolidBrush panelBg = new SolidBrush(Color.White))

            using (Pen panelBorder = new Pen(Color.FromArgb(234, 236, 240), 1f))

            {

                g.FillRectangle(panelBg, panelX, 0, panelW, height);

                g.DrawLine(panelBorder, panelX, 0, panelX, height);

            }



            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            g.SmoothingMode = SmoothingMode.AntiAlias;



            var rpClip = g.Save();

            g.SetClip(new RectangleF(panelX, 0, panelW, height), CombineMode.Intersect);

            try

            {

                GetCachedDriveUsage(out string driveLetter, out double usedGb, out double totalGb, out int usedPct);

                float expPx = panelX + 20f;

                float expPw = ExpandedRightPanelWidth - 40f;



                // ── 1. PROMO CARD (smoothly morphs 240x118 <-> 42x42 icon card) ──

                float promoX = (cx - 21f) + (expPx - (cx - 21f)) * t;

                float promoW = 42f + (expPw - 42f) * t;

                float promoH = 42f + (118f - 42f) * t;

                RectangleF promoRect = new RectangleF(promoX, 70f, promoW, promoH);

                using (GraphicsPath pp = CreateRoundedRectanglePath(promoRect, 12f))

                using (LinearGradientBrush promoBg = new LinearGradientBrush(promoRect,

                    (HoverPromoBtn && t < 0.5f) ? Color.FromArgb(219, 234, 254) : Color.FromArgb(238, 242, 255),

                    (HoverPromoBtn && t < 0.5f) ? Color.FromArgb(191, 219, 254) : Color.FromArgb(224, 231, 255), 135f))

                {

                    g.FillPath(promoBg, pp);

                    if (aIn > 8)

                    {

                        using (Pen cBd = new Pen(Color.FromArgb(aIn, HoverPromoBtn ? Color.FromArgb(59, 130, 246) : Color.FromArgb(199, 210, 254)), 1f))

                            g.DrawPath(cBd, pp);

                    }



                    if (aOut > 4)

                    {

                        var pClip = g.Save();

                        g.SetClip(pp, CombineMode.Intersect);

                        using (Font promoTitleFont = new Font("Segoe UI", 10f, FontStyle.Bold))

                        using (Font promoSubFont = new Font("Segoe UI", 8f, FontStyle.Regular))

                        using (SolidBrush promoTitleB = new SolidBrush(Color.FromArgb(aOut, 30, 41, 59)))

                        using (SolidBrush promoSubB = new SolidBrush(Color.FromArgb(aOut, 71, 85, 105)))

                        {

                            g.DrawString(AppL10n.T("Làm việc hiệu quả hơn", "Work smarter"), promoTitleFont, promoTitleB, promoX + 14f, 82f);

                            g.DrawString(AppL10n.T("Tận dụng các mẫu tài liệu có sẵn\nđể tiết kiệm thời gian và nâng cao\nhiệu suất công việc.", "Use ready-made templates to save\ntime and boost your everyday\nproductivity."), promoSubFont, promoSubB, promoX + 14f, 104f);

                        }

                        RectangleF ctaBtn = new RectangleF(promoX + 14f, 150f, Math.Max(24f, Math.Min(130f, promoW - 28f)), 28f);

                        using (GraphicsPath ctap = CreateRoundedRectanglePath(ctaBtn, 6f))

                        using (SolidBrush ctaBg = new SolidBrush(Color.FromArgb(aOut, HoverPromoBtn ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235))))

                        using (Font ctaFont = new Font("Segoe UI", 8.2f, FontStyle.Bold))

                        using (SolidBrush ctaTxt = new SolidBrush(Color.FromArgb(aOut, 255, 255, 255)))

                        {

                            g.FillPath(ctaBg, ctap);

                            StringFormat csf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                            bool showBackToHome = (ActiveTab == 0 && IsTemplateDiscoveryMode);

                            g.DrawString(showBackToHome ? AppL10n.T("⌂ Về Trang chủ", "⌂ Back Home") : AppL10n.T("🗂 Khám phá mẫu", "🗂 Browse Templates"), ctaFont, ctaTxt, ctaBtn, csf);

                        }

                        g.Restore(pClip);

                    }

                    if (aIn > 4)

                    {

                        bool showBackToHome = (ActiveTab == 0 && IsTemplateDiscoveryMode);

                        Color icCol = Color.FromArgb(aIn, HoverPromoBtn ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235));

                        FluentIconRenderer.DrawGlyph(g, showBackToHome ? '\uE80F' : '\uE8A9', promoRect, icCol, 18f);

                    }

                }



                // ── 1B. ĐƯỢC CHIA SẺ (200 MB Cloud) dưới thẻ Mẫu ──

                float shX = (cx - 24f) + (expPx - (cx - 24f)) * t;
                float shY = 122f + (198f - 122f) * t;
                float shW = 48f + (expPw - 48f) * t;
                float shH = 62f + (64f - 62f) * t;
                RectangleF sharedCardRect = new RectangleF(shX, shY, shW, shH);
                RightPanelSharedBtnRect = sharedCardRect;

                bool isSharedActive = (ActiveTab == 4);
                bool isSharedHover = HoverRightPanelSharedBtn;
                int shInCount = (DatabaseAuthService.IsLoggedIn && CloudShareService.InboxItems != null) ? CloudShareService.InboxItems.Count : 0;
                long cUsed = DatabaseAuthService.IsLoggedIn ? CloudShareService.SenderUsedBytes : 0L;
                double cRatio = Math.Min(1.0, Math.Max(0.0, (double)cUsed / CloudShareService.CloudQuotaBytes));
                int cPct = (int)Math.Round(cRatio * 100.0);

                if (t > 0.25f)
                {
                    int cardAlpha = (int)Math.Round(255f * Math.Clamp((t - 0.25f) / 0.45f, 0f, 1f));
                    Color shBgCol = isSharedActive ? Color.FromArgb(cardAlpha, 239, 246, 255) : (isSharedHover ? Color.FromArgb(cardAlpha, 241, 245, 249) : Color.FromArgb(cardAlpha, 248, 250, 252));
                    Color shBdCol = isSharedActive ? Color.FromArgb(cardAlpha, 37, 99, 235) : (isSharedHover ? Color.FromArgb(cardAlpha, 147, 197, 253) : Color.FromArgb(cardAlpha, 226, 232, 240));
                    using (GraphicsPath shp = CreateRoundedRectanglePath(sharedCardRect, 11f))
                    using (SolidBrush shBg = new SolidBrush(shBgCol))
                    using (Pen shBd = new Pen(shBdCol, isSharedActive ? 1.5f : 1f))
                    {
                        g.FillPath(shBg, shp);
                        g.DrawPath(shBd, shp);
                    }
                }

                if (aOut > 4)
                {
                    var sClip = g.Save();
                    using (GraphicsPath shp2 = CreateRoundedRectanglePath(sharedCardRect, 11f))
                        g.SetClip(shp2, CombineMode.Intersect);

                    Color icCol = Color.FromArgb(aOut, isSharedActive ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235));
                    FluentIconRenderer.DrawGlyph(g, '\uE72D', new RectangleF(shX + 10f, shY + 9f, 20f, 20f), icCol, 15f);

                    using (Font shTitleF = new Font("Segoe UI", 9.3f, FontStyle.Bold))
                    using (SolidBrush shTitleB = new SolidBrush(Color.FromArgb(aOut, isSharedActive ? Color.FromArgb(29, 78, 216) : Color.FromArgb(30, 41, 59))))
                    {
                        g.DrawString(AppL10n.T("Được chia sẻ", "Shared"), shTitleF, shTitleB, shX + 34f, shY + 10f);
                    }

                    if (shInCount > 0)
                    {
                        RectangleF badgeR = new RectangleF(shX + shW - 36f, shY + 10f, 26f, 18f);
                        using (GraphicsPath bp = CreateRoundedRectanglePath(badgeR, 9f))
                        using (SolidBrush bb = new SolidBrush(Color.FromArgb(aOut, 37, 99, 235)))
                        using (Font bf = new Font("Segoe UI", 7.6f, FontStyle.Bold))
                        using (SolidBrush bt = new SolidBrush(Color.FromArgb(aOut, 255, 255, 255)))
                        {
                            g.FillPath(bb, bp);
                            g.DrawString(shInCount.ToString(), bf, bt, badgeR, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                        }
                    }

                    float qBarW = Math.Max(20f, shW - 24f);
                    RectangleF qTrack = new RectangleF(shX + 12f, shY + 46f, qBarW, 5f);
                    using (GraphicsPath qp = CreateRoundedRectanglePath(qTrack, 2.5f))
                    using (SolidBrush qb = new SolidBrush(Color.FromArgb(aOut, 226, 232, 240)))
                        g.FillPath(qb, qp);

                    if (cRatio > 0)
                    {
                        RectangleF qFill = new RectangleF(qTrack.X, qTrack.Y, Math.Max(5f, (float)(qBarW * cRatio)), 5f);
                        Color qc = cRatio >= 0.99 ? Color.FromArgb(aOut, 220, 38, 38) : (cRatio >= 0.80 ? Color.FromArgb(aOut, 217, 119, 6) : Color.FromArgb(aOut, 37, 99, 235));
                        using (GraphicsPath qfp = CreateRoundedRectanglePath(qFill, 2.5f))
                        using (SolidBrush qfb = new SolidBrush(qc))
                            g.FillPath(qfb, qfp);
                    }

                    using (Font qSubF = new Font("Segoe UI", 7.6f, FontStyle.Regular))
                    using (SolidBrush qSubB = new SolidBrush(Color.FromArgb(aOut, cRatio >= 0.99 ? Color.FromArgb(185, 28, 28) : Color.FromArgb(100, 116, 139))))
                    {
                        string qSubTxt = DatabaseAuthService.IsLoggedIn
                            ? string.Format("{0} / 200 MB ({1}%)", CloudShareService.FormatSize(cUsed), cPct)
                            : AppL10n.T("Đăng nhập để chia sẻ", "Sign in to share");
                        g.DrawString(qSubTxt, qSubF, qSubB, shX + 12f, shY + 30f);
                    }

                    g.Restore(sClip);
                }

                if (aIn > 4)
                {
                    float ringSize = 36f + 6f * compactInF;
                    float ringY = 122f + (42f - ringSize) / 2f;
                    RectangleF cRingRect = new RectangleF(cx - ringSize / 2f, ringY, ringSize, ringSize);
                    Color cRingCol = Color.FromArgb(aIn,
                        cRatio >= 0.99 ? Color.FromArgb(220, 38, 38)
                        : (isSharedActive || isSharedHover ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235)));

                    using (Pen trackPen = new Pen(Color.FromArgb(aIn, 226, 232, 240), 4.5f))
                    using (Pen arcPen = new Pen(cRingCol, 4.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        if (isSharedActive || isSharedHover)
                        {
                            using (SolidBrush hRingBg = new SolidBrush(Color.FromArgb(aIn / 5, 37, 99, 235)))
                                g.FillEllipse(hRingBg, cRingRect);
                        }
                        g.DrawEllipse(trackPen, cRingRect);
                        float sweep = cRatio > 0 ? Math.Max(8f, (float)(360.0 * cRatio)) : 0f;
                        if (sweep > 0f)
                            g.DrawArc(arcPen, cRingRect, -90f, sweep);
                    }

                    using (Font pctFont = new Font("Segoe UI", 8.6f, FontStyle.Bold))
                    using (SolidBrush pctBrush = new SolidBrush(Color.FromArgb(aIn, isSharedActive ? Color.FromArgb(29, 78, 216) : Color.FromArgb(15, 23, 42))))
                    using (Font lblFont = new Font("Segoe UI", 7.6f, FontStyle.Bold))
                    using (SolidBrush lblBrush = new SolidBrush(Color.FromArgb(aIn, isSharedActive || isSharedHover ? Color.FromArgb(37, 99, 235) : Color.FromArgb(100, 116, 139))))
                    {
                        StringFormat csf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(string.Format("{0}%", cPct), pctFont, pctBrush, cRingRect, csf);
                        g.DrawString("Cloud", lblFont, lblBrush, new RectangleF(panelX + 2f, 167f, panelW - 4f, 16f), csf);
                    }

                    if (shInCount > 0)
                    {
                        using (SolidBrush dotB = new SolidBrush(Color.FromArgb(aIn, 37, 99, 235)))
                            g.FillEllipse(dotB, cRingRect.Right - 4f, cRingRect.Top - 1f, 8f, 8f);
                    }
                }

                // ── 2. STORAGE (smooth morph: wide bar -> centered 42x42 circular ring & %) ──

                if (aOut > 4)

                {

                    float storeY = 276f - (1f - t) * 28f;

                    float barX = (cx - 22f) + (expPx - (cx - 22f)) * t;

                    float barW = 44f + (expPw - 44f) * t;

                    using (Font storeTitleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))

                    using (Font storeSubFont = new Font("Segoe UI", 8f, FontStyle.Regular))

                    using (SolidBrush storeTitleB = new SolidBrush(Color.FromArgb(aOut, 30, 41, 59)))

                    using (SolidBrush linkB = new SolidBrush(Color.FromArgb(aOut, HoverStorageManage ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235))))

                    {

                        g.DrawString(string.Format(AppL10n.T("Dung lượng ổ đĩa ({0})", "Drive Storage ({0})"), driveLetter), storeTitleFont, storeTitleB, barX, storeY);

                        if (barW > 140f)

                            g.DrawString(AppL10n.T("Quản lý", "Manage"), storeSubFont, linkB, barX + barW - 48f, storeY + 2f);

                    }

                    float trackY = storeY + 22f;

                    RectangleF trackRect = new RectangleF(barX, trackY, barW, 7f);

                    using (GraphicsPath trp = CreateRoundedRectanglePath(trackRect, 3.5f))

                    using (SolidBrush trBg = new SolidBrush(Color.FromArgb(aOut, 226, 232, 240)))

                    {

                        g.FillPath(trBg, trp);

                    }

                    float fillW = Math.Max(7f, barW * (usedPct / 100f));

                    RectangleF fillRect = new RectangleF(barX, trackY, fillW, 7f);

                    using (GraphicsPath flp = CreateRoundedRectanglePath(fillRect, 3.5f))

                    using (SolidBrush flBg = new SolidBrush(Color.FromArgb(aOut, usedPct > 90 ? Color.FromArgb(239, 68, 68) : Color.FromArgb(37, 99, 235))))

                    {

                        g.FillPath(flBg, flp);

                    }

                    using (Font storeInfoFont = new Font("Segoe UI", 8f, FontStyle.Regular))

                    using (SolidBrush storeInfoB = new SolidBrush(Color.FromArgb(aOut, 100, 116, 139)))

                    {

                        g.DrawString(string.Format("{0} GB / {1} GB", usedGb, totalGb), storeInfoFont, storeInfoB, barX, trackY + 12f);

                        if (barW > 100f)

                            g.DrawString(string.Format("{0}%", usedPct), storeInfoFont, storeInfoB, barX + barW - 28f, trackY + 12f);

                    }

                }

                if (aIn > 4)

                {

                    float ringSize = 36f + 6f * compactInF;

                    float ringY = 194f + (42f - ringSize) / 2f;

                    RectangleF ringRect = new RectangleF(cx - ringSize / 2f, ringY, ringSize, ringSize);

                    Color ringColor = Color.FromArgb(aIn, usedPct > 90 ? Color.FromArgb(239, 68, 68) : (HoverStorageManage ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235)));

                    using (Pen trackPen = new Pen(Color.FromArgb(aIn, 226, 232, 240), 4.5f))

                    using (Pen arcPen = new Pen(ringColor, 4.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })

                    {

                        g.DrawEllipse(trackPen, ringRect);

                        float sweep = Math.Max(6f, 360f * (usedPct / 100f));

                        g.DrawArc(arcPen, ringRect, -90f, sweep);

                    }

                    using (Font pctFont = new Font("Segoe UI", 8.8f, FontStyle.Bold))

                    using (SolidBrush pctBrush = new SolidBrush(Color.FromArgb(aIn, 15, 23, 42)))

                    using (Font driveFont = new Font("Segoe UI", 7.8f, FontStyle.Bold))

                    using (SolidBrush driveBrush = new SolidBrush(Color.FromArgb(aIn, HoverStorageManage ? Color.FromArgb(37, 99, 235) : Color.FromArgb(100, 116, 139))))

                    {

                        StringFormat csf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                        g.DrawString(string.Format("{0}%", usedPct), pctFont, pctBrush, ringRect, csf);

                        g.DrawString(driveLetter, driveFont, driveBrush, new RectangleF(panelX + 2f, 239f, panelW - 4f, 16f), csf);

                    }

                }



                // ── 3. ACCOUNT CARD (smoothly morphs wide card <-> slim compact avatar & button) ──

                bool isLoggedIn = DatabaseAuthService.IsLoggedIn;

                var curUser = DatabaseAuthService.CurrentUser;

                float authY = 266f + (338f - 266f) * t;



                if (!isLoggedIn || curUser == null)

                {

                    _compactRightAvatarRect = RectangleF.Empty;

                    RightPanelSignOutBtnRect = RectangleF.Empty;

                    float cardW = 48f + (expPw - 48f) * t;

                    float cardH = 54f + (114f - 54f) * t;

                    float cardX = (cx - 24f) + (expPx - (cx - 24f)) * t;

                    RectangleF authCardRect = new RectangleF(cardX, authY, cardW, cardH);

                    using (GraphicsPath acp = CreateRoundedRectanglePath(authCardRect, 10f))

                    using (SolidBrush acBg = new SolidBrush(Color.FromArgb(248, 250, 252)))

                    using (Pen acBorder = new Pen(Color.FromArgb(226, 232, 240), 1f))

                    {

                        g.FillPath(acBg, acp);

                        g.DrawPath(acBorder, acp);



                        if (aOut > 4)

                        {

                            var aClip = g.Save();

                            g.SetClip(acp, CombineMode.Intersect);

                            RectangleF userIconCircle = new RectangleF(cardX + 12f, authY + 12f, 28f, 28f);

                            using (SolidBrush cirBg = new SolidBrush(Color.FromArgb(aOut, 238, 242, 255)))

                                g.FillEllipse(cirBg, userIconCircle);

                            FluentIconRenderer.DrawGlyph(g, '\uE80F', userIconCircle, Color.FromArgb(aOut, 37, 99, 235), 15f);

                            using (Font aTitleFont = new Font("Segoe UI", 9.2f, FontStyle.Bold))

                            using (Font aSubFont = new Font("Segoe UI", 7.8f, FontStyle.Regular))

                            using (SolidBrush aTitleB = new SolidBrush(Color.FromArgb(aOut, 30, 41, 59)))

                            using (SolidBrush aSubB = new SolidBrush(Color.FromArgb(aOut, 100, 116, 139)))

                            {

                                g.DrawString(AppL10n.T("Tài khoản ZenQ", "ZenQ Account"), aTitleFont, aTitleB, cardX + 46f, authY + 10f);

                                g.DrawString(AppL10n.T("Đăng nhập để đăng mẫu & Cloud", "Sign in for templates & Cloud"), aSubFont, aSubB, cardX + 46f, authY + 27f);

                            }

                            g.Restore(aClip);

                        }

                    }



                    float abW = 38f + ((expPw - 24f) - 38f) * t;

                    float abH = 38f + (34f - 38f) * t;

                    float abX = (cx - 19f) + ((cardX + 12f) - (cx - 19f)) * t;

                    float abY = (authY + 8f) + ((authY + 54f) - (authY + 8f)) * t;

                    RightPanelAuthBtnRect = new RectangleF(abX, abY, abW, abH);

                    using (GraphicsPath authBtnPath = CreateRoundedRectanglePath(RightPanelAuthBtnRect, 8f - 2f * t))

                    using (SolidBrush authBtnBrush = new SolidBrush(HoverRightPanelAuthBtn ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235)))

                    {

                        g.FillPath(authBtnBrush, authBtnPath);

                    }

                    if (aOut > 4 && abW > 80f)

                    {

                        using (Font btnFont = new Font("Segoe UI", 9.2f, FontStyle.Bold))

                        using (SolidBrush btnTxt = new SolidBrush(Color.FromArgb(aOut, 255, 255, 255)))

                        {

                            StringFormat bsf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                            g.DrawString(AppL10n.T("Đăng nhập / Đăng ký", "Sign In / Sign Up"), btnFont, btnTxt, RightPanelAuthBtnRect, bsf);

                        }

                    }

                    if (aIn > 4)

                    {

                        FluentIconRenderer.DrawGlyph(g, '\uE77B', RightPanelAuthBtnRect, Color.FromArgb(aIn, 255, 255, 255), 17f);

                    }

                }

                else

                {

                    RightPanelAuthBtnRect = RectangleF.Empty;

                    float cardW = 48f + (expPw - 48f) * t;

                    float cardH = 82f + (138f - 82f) * t;

                    float cardX = (cx - 24f) + (expPx - (cx - 24f)) * t;

                    RectangleF authCardRect = new RectangleF(cardX, authY, cardW, cardH);

                    using (GraphicsPath acp = CreateRoundedRectanglePath(authCardRect, 10f))

                    using (SolidBrush acBg = new SolidBrush(Color.FromArgb(248, 250, 252)))

                    using (Pen acBorder = new Pen(curUser.IsAdmin ? Color.FromArgb(254, 202, 202) : (curUser.IsCreator ? Color.FromArgb(221, 214, 254) : Color.FromArgb(226, 232, 240)), 1f))

                    {

                        g.FillPath(acBg, acp);

                        g.DrawPath(acBorder, acp);



                        // Monotonically gliding circular avatar (zero jump!)

                        float avSize = 32f + 2f * t;

                        float avX = (cx - avSize / 2f) + ((cardX + 12f) - (cx - avSize / 2f)) * t;

                        float avY = (authY + 8f) + ((authY + 12f) - (authY + 8f)) * t;

                        RectangleF userAvatarCircle = new RectangleF(avX, avY, avSize, avSize);

                        _compactRightAvatarRect = (t < 0.5f) ? userAvatarCircle : RectangleF.Empty;



                        Color avBgColor = curUser.IsAdmin ? Color.FromArgb(220, 38, 38) : (curUser.IsCreator ? Color.FromArgb(124, 58, 237) : Color.FromArgb(37, 99, 235));

                        using (SolidBrush avBg = new SolidBrush(avBgColor))

                        using (Font avFont = new Font("Segoe UI", 10.2f, FontStyle.Bold))

                        using (SolidBrush avTxt = new SolidBrush(Color.White))

                        {

                            g.FillEllipse(avBg, userAvatarCircle);

                            StringFormat asf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                            g.DrawString(curUser.Initials, avFont, avTxt, userAvatarCircle, asf);

                        }



                        if (aOut > 4)

                        {

                            var aClip = g.Save();

                            g.SetClip(acp, CombineMode.Intersect);

                            using (Font nameFont = new Font("Segoe UI", 9.2f, FontStyle.Bold))

                            using (Font userFont = new Font("Segoe UI", 7.8f, FontStyle.Regular))

                            using (SolidBrush nameB = new SolidBrush(Color.FromArgb(aOut, 15, 23, 42)))

                            using (SolidBrush userB = new SolidBrush(Color.FromArgb(aOut, 100, 116, 139)))

                            {

                                string displayName = !string.IsNullOrWhiteSpace(curUser.FullName) ? curUser.FullName : curUser.Username;

                                g.DrawString(displayName, nameFont, nameB, new RectangleF(cardX + 52f, authY + 11f, Math.Max(10f, cardW - 58f), 18f), new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });

                                g.DrawString("@" + curUser.Username, userFont, userB, cardX + 52f, authY + 28f);

                            }



                            RectangleF rolePillRect = new RectangleF(cardX + 12f, authY + 54f, Math.Max(16f, cardW - 24f), 24f);

                            Color pillBg = curUser.IsAdmin ? Color.FromArgb(aOut, 254, 242, 242) : (curUser.IsCreator ? Color.FromArgb(aOut, 245, 243, 255) : Color.FromArgb(aOut, 241, 245, 249));

                            Color pillBorder = curUser.IsAdmin ? Color.FromArgb(aOut, 254, 202, 202) : (curUser.IsCreator ? Color.FromArgb(aOut, 221, 214, 254) : Color.FromArgb(aOut, 226, 232, 240));

                            Color pillTxtColor = curUser.IsAdmin ? Color.FromArgb(aOut, 185, 28, 28) : (curUser.IsCreator ? Color.FromArgb(aOut, 109, 40, 217) : Color.FromArgb(aOut, 51, 65, 85));

                            using (GraphicsPath pillPath = CreateRoundedRectanglePath(rolePillRect, 5f))

                            using (SolidBrush pBg = new SolidBrush(pillBg))

                            using (Pen pBorder = new Pen(pillBorder, 1f))

                            using (Font pFont = new Font("Segoe UI", 7.8f, FontStyle.Bold))

                            using (SolidBrush pTxt = new SolidBrush(pillTxtColor))

                            {

                                g.FillPath(pBg, pillPath);

                                g.DrawPath(pBorder, pillPath);

                                StringFormat psf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                                g.DrawString(AppL10n.TrRole(curUser.RoleDisplayName), pFont, pTxt, rolePillRect, psf);

                            }

                            g.Restore(aClip);

                        }

                    }



                    // Sign-out button smoothly glides between compact icon & wide "Đăng xuất" pill

                    float soW = 36f + (64f - 36f) * t;

                    float soH = 24f;

                    float soX = (cx - 18f) + ((cardX + cardW - 76f) - (cx - 18f)) * t;

                    float soY = (authY + 48f) + ((authY + 92f) - (authY + 48f)) * t;

                    RightPanelSignOutBtnRect = new RectangleF(soX, soY, soW, soH);

                    using (GraphicsPath soPath = CreateRoundedRectanglePath(RightPanelSignOutBtnRect, 5f))

                    using (SolidBrush soBg = new SolidBrush(HoverRightPanelSignOutBtn ? Color.FromArgb(254, 242, 242) : Color.White))

                    using (Pen soBorder = new Pen(HoverRightPanelSignOutBtn ? Color.FromArgb(239, 68, 68) : Color.FromArgb(226, 232, 240), 1f))

                    {

                        g.FillPath(soBg, soPath);

                        g.DrawPath(soBorder, soPath);

                    }

                    if (aOut > 4 && soW > 48f)

                    {

                        using (Font soFont = new Font("Segoe UI", 7.8f, FontStyle.Regular))

                        using (SolidBrush soTxt = new SolidBrush(Color.FromArgb(aOut, HoverRightPanelSignOutBtn ? Color.FromArgb(220, 38, 38) : Color.FromArgb(100, 116, 139))))

                        {

                            StringFormat sosf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                            g.DrawString(AppL10n.T("Đăng xuất", "Sign Out"), soFont, soTxt, RightPanelSignOutBtnRect, sosf);

                        }

                    }

                    if (aIn > 4)

                    {

                        FluentIconRenderer.DrawGlyph(g, '\uE7E8', RightPanelSignOutBtnRect,

                            Color.FromArgb(aIn, HoverRightPanelSignOutBtn ? Color.FromArgb(220, 38, 38) : Color.FromArgb(100, 116, 139)), 12f);

                    }

                }



                // ── 4. Update button (smooth morph if available) ──

                bool hasNewUpdate = (UpdateManager.LastReleaseInfo != null && UpdateManager.LastReleaseInfo.HasUpdate);

                if (!hasNewUpdate)

                {

                    RightPanelUpdateBtnRect = RectangleF.Empty;

                    return;

                }



                float btnW = 38f + (126f - 38f) * t;

                float btnH = 28f;

                float btnX = (cx - 19f) + ((expPx + expPw - 126f) - (cx - 19f)) * t;

                float btnY = height - 40f;

                RightPanelUpdateBtnRect = new RectangleF(btnX, btnY, btnW, btnH);



                Color btnBg = HoverRightPanelUpdateBtn ? Color.FromArgb(185, 28, 28) : Color.FromArgb(220, 38, 38);

                Color btnBorder = Color.FromArgb(185, 28, 28);

                using (GraphicsPath btnPath = CreateRoundedRectanglePath(RightPanelUpdateBtnRect, btnH / 2f))

                using (SolidBrush bgB = new SolidBrush(btnBg))

                using (Pen borderP = new Pen(btnBorder, HoverRightPanelUpdateBtn ? 1.2f : 1f))

                {

                    g.FillPath(bgB, btnPath);

                    g.DrawPath(borderP, btnPath);

                }

                float iconSize = 14f;

                float iconX = (cx - iconSize / 2f) + ((btnX + 10f) - (cx - iconSize / 2f)) * t;

                float iconY = btnY + (btnH - iconSize) / 2f;

                DrawWin11UpdateIcon(g, iconX, iconY, iconSize, Color.White);

                if (aOut > 4 && btnW > 90f)

                {

                    using (Font btnF = new Font("Segoe UI", 8.2f, FontStyle.Bold))

                    using (SolidBrush textB = new SolidBrush(Color.FromArgb(aOut, 255, 255, 255)))

                        g.DrawString(AppL10n.T("Cập nhật ngay", "Update Now"), btnF, textB, iconX + iconSize + 6f, btnY + 6f);

                }

            }

            finally

            {

                g.Restore(rpClip);

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



            // 2.1 Sidebar clip & Collapse Toggle fallback (when drawn standalone; RibbonBar draws it on top header strip in AppForm)

            float expandT = SidebarExpandFactor;

            RectangleF toggleRect = GetSidebarToggleBounds(width, height);

            var sbClipState = g.Save();

            g.SetClip(new RectangleF(0, 0, SidebarWidth, height), CombineMode.Intersect);

            DrawSidebarToggleButton(g, toggleRect, HoverSidebarToggle);



            // 2.2 Nav items (1: Trang chủ, 2: Khám phá mẫu, 3: Tài liệu của tôi, 4: Thùng rác)

            RectangleF tab0Rect = GetMyDocsTabBounds(width, height);

            DrawSidebarNavItem(g, tab0Rect, AppL10n.T("Trang chủ", "Home"), (gr, ix, iy, isz, icol) => DrawVectorHomeIcon(gr, ix, iy, isz, icol), ActiveTab == 0 && !IsTemplateDiscoveryMode, HoverTabIndex == 0);



            RectangleF tmplNavRect = GetTemplateDiscoveryNavBounds(width, height);

            DrawSidebarNavItem(g, tmplNavRect, AppL10n.T("Khám phá mẫu", "Templates"), (gr, ix, iy, isz, icol) => DrawVectorTemplateIcon(gr, ix, iy, isz, icol), ActiveTab == 0 && IsTemplateDiscoveryMode, HoverTabIndex == 10);



            RectangleF navDocRect = GetOpenDocNavBounds(width, height);

            DrawSidebarNavItem(g, navDocRect, AppL10n.T("Tài liệu của tôi", "My Documents"), (gr, ix, iy, isz, icol) => DrawVectorFolderIcon(gr, ix, iy, isz, icol), ActiveTab == 3, HoverTabIndex == 3 || HoverSidebarDoc);



            RectangleF tab1Rect = GetTrashTabBounds(width, height);

            DrawSidebarNavItem(g, tab1Rect, AppL10n.T("Thùng rác", "Trash"), (gr, ix, iy, isz, icol) => DrawVectorTrashIcon(gr, ix, iy, isz, icol), ActiveTab == 1, HoverTabIndex == 1);



            // 2.3 "Ứng dụng" section (CLICKABLE SHORTCUTS TO CREATE DOCUMENTS)

            float appSectionY = 226f;

            if (expandT > 0.35f)

            {

                int secAlpha = (int)Math.Round(255f * Math.Clamp((expandT - 0.35f) / 0.65f, 0f, 1f));

                using (Font secLabelFont = new Font("Segoe UI", 8.2f, FontStyle.Bold))

                using (SolidBrush secLabelBrush = new SolidBrush(Color.FromArgb(secAlpha, 148, 163, 184)))

                {

                    g.DrawString(AppL10n.T("ỨNG DỤNG", "APPS"), secLabelFont, secLabelBrush, 20f, appSectionY);

                }

            }

            else

            {

                using (Pen secDivPen = new Pen(Color.FromArgb(234, 236, 240), 1f))

                {

                    g.DrawLine(secDivPen, 14f, appSectionY + 8f, SidebarWidth - 14f, appSectionY + 8f);

                }

            }

            DrawSidebarAppNavItem(g, GetAppNavBounds(0, width, height), AppL10n.T("Tài liệu", "Document"), OfficeAppMode.Writer, HoverSidebarApp == 0);

            DrawSidebarAppNavItem(g, GetAppNavBounds(1, width, height), AppL10n.T("Bảng tính", "Spreadsheet"), OfficeAppMode.Calc, HoverSidebarApp == 1);

            DrawSidebarAppNavItem(g, GetAppNavBounds(2, width, height), AppL10n.T("Thuyết trình", "Presentation"), OfficeAppMode.Slides, HoverSidebarApp == 2);

            DrawSidebarAppNavItem(g, GetAppNavBounds(3, width, height), "PDF", OfficeAppMode.Pdf, HoverSidebarApp == 3);

            DrawSidebarAppNavItem(g, GetAppNavBounds(4, width, height), AppL10n.T("Đồ họa & Ảnh", "Design & Photo"), OfficeAppMode.Photo, HoverSidebarApp == 4);



            // 2.4 Settings at bottom

            float sepY = height - 104f;

            using (Pen sepPen = new Pen(Color.FromArgb(234, 236, 240), 1f))

            {

                g.DrawLine(sepPen, 16f, sepY, SidebarWidth - 16f, sepY);

            }

            RectangleF bugReportRect = GetBugReportTabBounds(width, height);

            DrawSidebarNavItem(g, bugReportRect, AppL10n.T("Báo lỗi & Góp ý", "Feedback"), (gr, ix, iy, isz, icol) => DrawVectorFeedbackIcon(gr, ix + isz / 2f, iy + isz / 2f, isz, icol), false, HoverTabIndex == 4);



            RectangleF tab2Rect = GetSettingsTabBounds(width, height);

            DrawSidebarNavItem(g, tab2Rect, AppL10n.T("Cài đặt", "Settings"), (gr, ix, iy, isz, icol) => DrawVectorGearIcon(gr, ix + isz / 2f, iy + isz / 2f, isz, icol), ActiveTab == 2, HoverTabIndex == 2);

            g.Restore(sbClipState);



            // 3. CENTER CONTENT

            float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;

            GetCenterContentMetrics(width, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);



            // 3.1 Top bar: Search (ẩn trên trang Cài đặt để dành không gian cho Header Cài đặt)

            if (ActiveTab != 2)

            {

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

                        g.DrawString(AppL10n.T("Tìm kiếm tệp, thư mục, mẫu...", "Search files, folders, templates..."), searchFont, phBrush, textX, textY);

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

            }



            // Top bar icons: help

            RectangleF helpRect = GetHelpBtnBounds(width, height);

            if (HoverHelpBtn)

            {

                using (SolidBrush hb = new SolidBrush(Color.FromArgb(241, 245, 249)))

                {

                    g.FillEllipse(hb, helpRect);

                }

            }

            DrawVectorQuestionIcon(g, helpRect.X + helpRect.Width / 2f, helpRect.Y + helpRect.Height / 2f, 16f, Color.FromArgb(100, 116, 139));



            GraphicsState homeCenterClip = null;

            // 4. WELCOME BANNER (Tab 0 only)

            if (ActiveTab == 0)

            {

                if (IsTemplateDiscoveryMode)

                {

                    // THAY THẾ BANNER, CÁC APP VÀ PHẦN DƯỚI BẰNG TRANG KHÁM PHÁ MẪU

                    // Cả 2 thanh bên trái và phải được giữ nguyên hoàn toàn

                    _templateDiscoveryView.Draw(g, startX, 68f, contentW, height - 68f, LastMouseX, LastMouseY);

                }

                else

                {

                    float hScrollShift = currentScroll;

                    RectangleF homeCenterVp = new RectangleF(startX - 4f, 66f, contentW + 14f, Math.Max(100f, height - 70f));

                    homeCenterClip = g.Save();

                    g.SetClip(homeCenterVp);

                    RectangleF bannerRect = new RectangleF(startX, 68f - hScrollShift, contentW, bannerH);

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
                            g.DrawString(AppL10n.T("Chào mừng bạn quay trở lại!", "Welcome back!"), bannerTitleFont, bannerTxtBrush, startX + 28f, bannerRect.Y + 28f);
                            using (SolidBrush bannerSubBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
                            {
                                g.DrawString(AppL10n.T("Bắt đầu công việc của bạn với các công cụ văn phòng mạnh mẽ và linh hoạt.", "Get started with powerful and flexible office productivity tools."), bannerSubFont, bannerSubBrush, startX + 28f, bannerRect.Y + 60f);
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

            }



            // 5. CREATION CARDS (Chỉ hiển thị tại Trang chủ khi không ở chế độ Khám phá mẫu)

            if (ActiveTab == 0 && !IsTemplateDiscoveryMode)

            {

                RectangleF c0 = GetCardBounds(0, width, height);

                RectangleF c1 = GetCardBounds(1, width, height);

                RectangleF c2 = GetCardBounds(2, width, height);

                RectangleF c3 = GetCardBounds(3, width, height);

                RectangleF c4 = GetCardBounds(4, width, height);

                DrawCreationCard(g, c0, AppL10n.T("Tạo tài liệu", "New Document"), AppL10n.T("Soạn thảo văn bản", "Word processing"), OfficeAppMode.Writer, HoverCardIndex == 0);

                DrawCreationCard(g, c1, AppL10n.T("Tạo bảng tính", "New Sheet"), AppL10n.T("Xử lý dữ liệu", "Spreadsheets"), OfficeAppMode.Calc, HoverCardIndex == 1);

                DrawCreationCard(g, c2, AppL10n.T("Tạo thuyết trình", "New Slides"), AppL10n.T("Trình bày ý tưởng", "Presentations"), OfficeAppMode.Slides, HoverCardIndex == 2);

                DrawCreationCard(g, c3, AppL10n.T("Tạo từ PDF", "Open PDF"), AppL10n.T("Chỉnh sửa, chuyển đổi", "Edit & convert"), OfficeAppMode.Pdf, HoverCardIndex == 3);

                DrawCreationCard(g, c4, AppL10n.T("Đồ họa & Ảnh", "Design & Photo"), AppL10n.T("Thiết kế lớp & PSD", "Layers & PSD"), OfficeAppMode.Photo, HoverCardIndex == 4);

            }



            // 6. MAIN CONTENT AREA

            if (ActiveTab == 2)

            {

                DrawSettingsHeader(g, width);

                float px, pw, barRightX;

                GetSettingsLayoutMetrics(width, out px, out pw, out barRightX);

                RectangleF setViewport = new RectangleF(SidebarWidth + 2f, 122f, Math.Max(200f, width - SidebarWidth - 4f), Math.Max(100f, height - 126f));

                GraphicsState gs = g.Save();

                g.SetClip(setViewport);

                DrawSettingsDashboard(g, px, pw, 134f - currentScroll);

                g.Restore(gs);

                DrawVerticalScrollBar(g, setViewport, Math.Max(_settingsContentTotalH, GetSettingsContentHeight(width)), currentScroll, maxScroll, barRightX);

            }

            else if (ActiveTab == 4)

            {

                _sharedFilesView.Draw(g, startX, 68f, contentW, Math.Max(200f, height - 76f), currentScroll, SearchKeyword);

            }

            else if (ActiveTab != 0 || !IsTemplateDiscoveryMode)

            {

                bool homeWholeScroll = (ActiveTab == 0 && !IsTemplateDiscoveryMode);

                float hScrollShift = homeWholeScroll ? currentScroll : 0f;

                float drawFileListY = fileListY - hScrollShift;

                float drawColHeaderY = colHeaderY - hScrollShift;

                float drawListTopY = listTopY - hScrollShift;

                // File list (fileListY, colHeaderY, listTopY from GetCenterContentMetrics)

                if (ActiveTab == 3)

                {

                    using (Font secHeaderFont = new Font("Segoe UI", 13f, FontStyle.Bold))

                    using (SolidBrush secHeaderBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))

                    {

                        g.DrawString(AppL10n.T("Tài liệu của tôi", "My Documents"), secHeaderFont, secHeaderBrush, startX, fileListY - 2f);

                    }

                    using (Font subFont = new Font("Segoe UI", 8.8f, FontStyle.Regular))

                    using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))

                    {

                        g.DrawString(AppL10n.T("Quản lý và mở nhanh tất cả các tệp tài liệu trong máy của bạn", "Manage and quickly open all local documents on your PC"), subFont, subBrush, startX, fileListY + 22f);

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

                        g.DrawString(AppL10n.T("Mở tệp từ máy...", "Browse local files..."), btnF, btnB, MyDocsOpenFileBtnRect.X + 34f, MyDocsOpenFileBtnRect.Y + 7f);

                    }



                    // Filter pills

                    MyDocsFilterPills.Clear();

                    string[] labels = new string[] { AppL10n.T("Tất cả", "All"), AppL10n.T("Gần đây", "Recent"), AppL10n.T("Đã gắn sao", "Starred"), AppL10n.T("Văn bản (.docx)", "Docs (.docx)"), AppL10n.T("Bảng tính (.xlsx)", "Sheets (.xlsx)"), AppL10n.T("Thuyết trình (.pptx)", "Slides (.pptx)"), "PDF (.pdf)", AppL10n.T("Đồ họa Vector (.svg)", "Vector (.svg)") };

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
                        string headerText = (ActiveTab == 1) ? AppL10n.T("Thùng rác", "Trash") :
                                            ((SubFilter == 2) ? AppL10n.T("Tài liệu đã gắn dấu sao", "Starred Documents") :
                                            ((SubFilter == 1) ? AppL10n.T("Tệp gần đây", "Recent Files") : AppL10n.T("Tất cả tệp gần đây", "All Recent Files")));
                        g.DrawString(headerText, secHeaderFont, secHeaderBrush, startX, drawFileListY);
                    }
                    if (ActiveTab == 0)
                    {
                        using (Font linkFont = new Font("Segoe UI", 9f, FontStyle.Regular))
                        using (SolidBrush linkBrush = new SolidBrush(Color.FromArgb(37, 99, 235)))
                        {
                            string linkText = AppL10n.T("Xem tất cả →", "View all →");
                            SizeF linkSz = g.MeasureString(linkText, linkFont);
                            g.DrawString(linkText, linkFont, linkBrush, startX + contentW - linkSz.Width, drawFileListY + 4f);
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

                                g.DrawString(AppL10n.T("Dọn sạch thùng rác", "Empty Trash"), btnF, btnB, empBtn.X + 30f, empBtn.Y + 7f);

                            }

                        }

                    }

                }



                // Column headers (colHeaderY and listTopY already computed from GetCenterContentMetrics)

                using (Font thFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (SolidBrush thBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString(AppL10n.T("Tên tệp", "Name"), thFont, thBrush, startX + 16f, drawColHeaderY);
                    g.DrawString(AppL10n.T("Loại", "Type"), thFont, thBrush, startX + contentW * 0.50f, drawColHeaderY);
                    g.DrawString(AppL10n.T("Thời gian sửa đổi", "Date Modified"), thFont, thBrush, startX + contentW * 0.62f, drawColHeaderY);
                    g.DrawString(AppL10n.T("Kích thước", "Size"), thFont, thBrush, startX + contentW * 0.80f, drawColHeaderY);
                }
                using (Pen hLine = new Pen(Color.FromArgb(234, 236, 240), 1f))
                {
                    g.DrawLine(hLine, startX, drawColHeaderY + 20f, startX + contentW, drawColHeaderY + 20f);
                }



                RectangleF listViewport = homeWholeScroll

                    ? new RectangleF(startX, 66f, contentW, Math.Max(100f, height - 70f))

                    : new RectangleF(startX, listTopY, contentW, Math.Max(100f, height - listTopY - 4f));

                GraphicsState gs = null;

                if (!homeWholeScroll)

                {

                    gs = g.Save();

                    g.SetClip(listViewport);

                }



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

                            emptyMsg = string.Format(AppL10n.T("Không tìm thấy tài liệu nào khớp với từ khóa '{0}'.", "No documents match '{0}'."), SearchKeyword);

                        }

                        else

                        {

                            if (ActiveTab == 3)

                            {

                                if (SubFilter == 2 || SubFilter == 14)

                                    emptyMsg = AppL10n.T("Chưa có tài liệu nào được gắn dấu sao. Hãy nhấp vào biểu tượng ngôi sao ở danh sách tệp để đánh dấu tài liệu quan trọng!", "No starred documents yet. Click the star icon on any file to bookmark it here.");

                                else

                                    emptyMsg = AppL10n.T("Chưa có tài liệu nào trong mục này. Nhấn 'Mở tệp từ máy...' để thêm tài liệu hoặc tạo tệp mới từ Trang chủ!", "No files in this view yet. Click 'Browse local files...' or create a new file from Home.");

                            }

                            else if (ActiveTab == 0)

                            {

                                emptyMsg = AppL10n.T("Chưa có tệp nào gần đây. Hãy tạo tệp mới từ các chức năng ở trên!", "No recent files yet. Create a new document from the cards above!");

                            }

                            else

                            {

                                emptyMsg = AppL10n.T("Thùng rác đang trống.", "Trash is empty.");

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

                            string displayPath = !string.IsNullOrEmpty(doc.FilePath) ? doc.FilePath : AppL10n.T("(Bản nháp tự động lưu)", "(Auto-saved draft)");

                            RectangleF pathRect = new RectangleF(rowRect.X + 56f, rowRect.Y + 31f, titleMaxW, 16f);

                            using (SolidBrush pathB = new SolidBrush(Color.FromArgb(148, 163, 184)))

                            using (StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisPath, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center })

                            {

                                g.DrawString(displayPath, rPathFont, pathB, pathRect, sf);

                            }



                            // Type badge pill

                            string modeName = (doc.Mode == OfficeAppMode.Writer) ? AppL10n.T("Tài liệu", "Document") :

                                              ((doc.Mode == OfficeAppMode.Calc) ? AppL10n.T("Bảng tính", "Sheet") :

                                              ((doc.Mode == OfficeAppMode.Slides) ? AppL10n.T("Thuyết trình", "Slides") :

                                              ((doc.Mode == OfficeAppMode.Photo) ? AppL10n.T("Đồ họa PSD", "Photo PSD") : "PDF")));

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

                                g.DrawString(AppL10n.TrTodayDate(doc.LastModified), rFont, timeB, dateRect, sf);

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



                                // Nút Thùng rác (Xóa / Chuyển vào Thùng rác)

                                bool isRowTrashHov = (HoverRowTrash == i);

                                if (isRowTrashHov)

                                {

                                    using (SolidBrush hovBg = new SolidBrush(Color.FromArgb(254, 242, 242)))

                                    {

                                        g.FillEllipse(hovBg, dotX - 12f, dotCY - 12f, 24f, 24f);

                                    }

                                }

                                Color trashColor = isRowTrashHov ? Color.FromArgb(220, 38, 38) : Color.FromArgb(148, 163, 184);

                                DrawVectorTrashIcon(g, dotX - 8f, dotCY - 8f, 16f, trashColor);

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

                                    g.DrawString(AppL10n.T("Phục hồi", "Restore"), rFont, rtBrush, resBtn, rsf);

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

                                    g.DrawString(AppL10n.T("Xóa", "Delete"), rFont, ptBrush, purgeBtn, psf);

                                }

                            }

                        }

                    }

                }

                if (!homeWholeScroll && gs != null)

                {

                    g.Restore(gs);

                }

                else if (homeWholeScroll && homeCenterClip != null)

                {

                    g.Restore(homeCenterClip);

                    homeCenterClip = null;

                }



                float totalListH = homeWholeScroll
                    ? Math.Max(listViewport.Height + 1f, (listTopY - 68f) + Math.Max(1, currentList.Count) * rowTotalH + 36f)
                    : (currentList.Count * rowTotalH + 96f);
                DrawVerticalScrollBar(g, listViewport, totalListH, currentScroll, maxScroll, startX + contentW);

            }



            // 7. RIGHT SIDEBAR PANEL

            if (ActiveTab != 2)

            {

                DrawRightSidebarPanel(g, width, height);

            }

            DrawCollapsedSidebarTooltip(g, width, height);

        }



        private void DrawVerticalScrollBar(Graphics g, RectangleF viewport, float totalContentH, int currentScroll, int maxScroll, float barRightX)
        {
            if (maxScroll <= 0)
            {
                ScrollBarBounds = RectangleF.Empty;
                ScrollBarThumbBounds = RectangleF.Empty;
                return;
            }
            float trackH = Math.Max(1f, viewport.Height - 16f);
            float safeTotal = Math.Max(viewport.Height + 1f, totalContentH);
            float thumbH = Math.Max(30f, Math.Min(trackH - 10f, trackH * (viewport.Height / safeTotal)));
            float thumbY = viewport.Top + 8f + (currentScroll / (float)maxScroll) * (trackH - thumbH);
            ScrollBarBounds = new RectangleF(barRightX - 14f, viewport.Top + 4f, 18f, Math.Max(1f, viewport.Height - 8f));
            ScrollBarThumbBounds = new RectangleF(barRightX - 7f, thumbY, 5f, thumbH);

            if (IsHoveringScrollBar || IsDraggingScrollBar)
            {
                RectangleF trackRect = new RectangleF(barRightX - 9f, viewport.Top + 4f, 9f, Math.Max(1f, viewport.Height - 8f));
                using (GraphicsPath trackPath = CreateRoundedRectanglePath(trackRect, 4f))
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



        public void GetSettingsLayoutMetrics(int width, out float pageX, out float pageW, out float barRightX)
        {
            float avail = Math.Max(460f, width - SidebarWidth);
            float padX = (avail >= 960f) ? 36f : 22f;
            pageW = Math.Min(1260f, Math.Max(420f, avail - padX * 2f));
            pageX = SidebarWidth + Math.Max(padX, (avail - pageW) / 2f);
            barRightX = width - 8f;
        }

        private float GetSettingsContentHeight(int width)
        {
            float pageX, pageW, barRightX;
            GetSettingsLayoutMetrics(width, out pageX, out pageW, out barRightX);
            bool twoCol = (SettingsCategoryIndex == 0 && pageW >= 880f);
            float colW = twoCol ? (pageW - 20f) / 2f : Math.Min(pageW, 820f);
            float innerW = colW - 36f;

            float aiModelH = (innerW >= 450f) ? 92f : 128f;
            float cardAiH = 58f + 56f + aiModelH + 56f;
            float cardAppearH = 58f + 58f * 3f;
            float cardShortcutsH = 58f + ((innerW >= 360f) ? 148f : 224f);
            float cardSystemH = 58f + 56f * 6f;
            float cardAssocH = 58f + 56f * 4f + 62f;
            float cardAboutH = (pageW >= 640f) ? 84f : 118f;
            float gap = 18f;

            if (SettingsCategoryIndex == 1) return cardAiH + gap + cardAboutH + 36f;
            if (SettingsCategoryIndex == 2) return cardAppearH + 36f;
            if (SettingsCategoryIndex == 3) return cardSystemH + 36f;
            if (SettingsCategoryIndex == 4) return cardShortcutsH + gap + cardAssocH + 36f;

            if (twoCol)
            {
                float leftH = cardAiH + gap + cardAppearH + gap + cardShortcutsH;
                float rightH = cardSystemH + gap + cardAssocH;
                return Math.Max(leftH, rightH) + gap + cardAboutH + 36f;
            }
            return cardAiH + gap + cardAppearH + gap + cardSystemH + gap + cardShortcutsH + gap + cardAssocH + gap + cardAboutH + 36f;
        }

        private void ResetSettingsHitRects()
        {
            SettingAiCopilotToggleRect = RectangleF.Empty;
            SettingAiProviderGoogleRect = RectangleF.Empty;
            SettingAiProviderOpenAiRect = RectangleF.Empty;
            SettingAiProviderClaudeRect = RectangleF.Empty;
            SettingAiProviderOllamaRect = RectangleF.Empty;
            SettingAiConfigBtnRect = RectangleF.Empty;
            SettingRibbonIslandRect = RectangleF.Empty;
            SettingRibbonStandardRect = RectangleF.Empty;
            SettingThemeLightRect = RectangleF.Empty;
            SettingThemeDarkRect = RectangleF.Empty;
            SettingAutoSaveToggleRect = RectangleF.Empty;
            SettingAutoSave30Rect = RectangleF.Empty;
            SettingAutoSave60Rect = RectangleF.Empty;
            SettingAutoSave120Rect = RectangleF.Empty;
            SettingSmoothScrollRect = RectangleF.Empty;
            SettingRulerRect = RectangleF.Empty;
            SettingAutoCorrectViRect = RectangleF.Empty;
            SettingWordPredictionRect = RectangleF.Empty;
            SettingLangViRect = RectangleF.Empty;
            SettingLangEnRect = RectangleF.Empty;
            SettingCheckUpdateBtnRect = RectangleF.Empty;
            SettingHelpBtnRect = RectangleF.Empty;
            SettingShortcutWriterRect = RectangleF.Empty;
            SettingShortcutCalcRect = RectangleF.Empty;
            SettingShortcutSlidesRect = RectangleF.Empty;
            SettingShortcutPdfRect = RectangleF.Empty;
            SettingAssocWriterRect = RectangleF.Empty;
            SettingAssocCalcRect = RectangleF.Empty;
            SettingAssocSlidesRect = RectangleF.Empty;
            SettingAssocPdfRect = RectangleF.Empty;
            SettingAssocSetAllBtnRect = RectangleF.Empty;
            SettingAssocUnsetAllBtnRect = RectangleF.Empty;
            SettingAssocWinSettingsRect = RectangleF.Empty;
        }

        private Color LerpColor(Color a, Color b, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return Color.FromArgb(
                (int)Math.Round(a.A + (b.A - a.A) * t),
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }

        private float GetSettingHoverAnim(string key, bool hovered)
        {
            float tgt = hovered ? 1f : 0f;
            _settingHoverTarget[key] = tgt;
            if (!_settingHoverPos.TryGetValue(key, out float cur))
            {
                cur = tgt;
                _settingHoverPos[key] = cur;
            }
            return cur;
        }

        private float GetSettingToggleAnim(string key, bool isOn)
        {
            float tgt = isOn ? 1f : 0f;
            _settingToggleTarget[key] = tgt;
            if (!_settingTogglePos.TryGetValue(key, out float cur))
            {
                cur = tgt;
                _settingTogglePos[key] = cur;
            }
            return cur;
        }

        private void DrawSettingsHeader(Graphics g, int width)
        {
            float pageX, pageW, barRightX;
            GetSettingsLayoutMetrics(width, out pageX, out pageW, out barRightX);

            // Title badge + Title + Subtitle
            RectangleF badgeRect = new RectangleF(pageX, 20f, 38f, 38f);
            using (GraphicsPath bp = CreateRoundedRectanglePath(badgeRect, 10f))
            using (SolidBrush bb = new SolidBrush(Color.FromArgb(239, 246, 255)))
            using (Pen bpen = new Pen(Color.FromArgb(219, 234, 254), 1f))
            {
                g.FillPath(bb, bp);
                g.DrawPath(bpen, bp);
            }
            DrawVectorGearIcon(g, badgeRect.X + badgeRect.Width / 2f, badgeRect.Y + badgeRect.Height / 2f, 18f, Color.FromArgb(37, 99, 235));

            using (Font titleFont = new Font("Segoe UI", 15.5f, FontStyle.Bold))
            using (Font subFont = new Font("Segoe UI", 8.8f, FontStyle.Regular))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                g.DrawString(AppL10n.T("Cài đặt", "Settings"), titleFont, titleBrush, pageX + 48f, 17f);
                g.DrawString(AppL10n.T("Tùy chỉnh trợ lý AI, giao diện, soạn thảo và tích hợp hệ thống", "Customize AI assistant, appearance, editing, and system integration"), subFont, subBrush, pageX + 49f, 44f);
            }

            // Category Filter Segmented Bar with gliding active pill
            string[] catLabels = new string[]
            {
                AppL10n.T("Tất cả", "All"),
                AppL10n.T("Trí tuệ nhân tạo", "AI & Copilot"),
                AppL10n.T("Giao diện & Ngôn ngữ", "Appearance & Language"),
                AppL10n.T("Hệ thống & Soạn thảo", "System & Editing"),
                AppL10n.T("Lối tắt & Mặc định", "Shortcuts & Apps")
            };

            using (Font pillFont = new Font("Segoe UI", 8.8f, FontStyle.Regular))
            using (Font pillBoldFont = new Font("Segoe UI", 8.8f, FontStyle.Bold))
            {
                float pad = 4f;
                float innerGap = 4f;
                float[] tabWidths = new float[catLabels.Length];
                float totalTabsW = pad * 2f;
                for (int i = 0; i < catLabels.Length; i++)
                {
                    SizeF sz = g.MeasureString(catLabels[i], pillBoldFont);
                    tabWidths[i] = (float)Math.Ceiling(sz.Width) + 24f;
                    totalTabsW += tabWidths[i] + (i > 0 ? innerGap : 0f);
                }

                float maxStripW = pageW;
                float scale = totalTabsW > maxStripW ? (maxStripW - pad * 2f - innerGap * (catLabels.Length - 1)) / Math.Max(1f, totalTabsW - pad * 2f - innerGap * (catLabels.Length - 1)) : 1f;
                float stripW = Math.Min(maxStripW, totalTabsW);
                RectangleF stripRect = new RectangleF(pageX, 74f, stripW, 34f);

                using (GraphicsPath sp = CreateRoundedRectanglePath(stripRect, 9f))
                using (SolidBrush sbg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                using (Pen spen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.FillPath(sbg, sp);
                    g.DrawPath(spen, sp);
                }

                float tx = stripRect.X + pad;
                for (int i = 0; i < catLabels.Length; i++)
                {
                    float tw = tabWidths[i] * scale;
                    SettingsCategoryTabRects[i] = new RectangleF(tx, stripRect.Y + pad, tw, stripRect.Height - pad * 2f);
                    if (SettingsCategoryIndex == i)
                    {
                        _catPillTargetX = tx;
                        _catPillTargetW = tw;
                        if (_catPillCurX < 0f || _catPillCurW < 0f)
                        {
                            _catPillCurX = _catPillTargetX;
                            _catPillCurW = _catPillTargetW;
                        }
                    }
                    tx += tw + innerGap;
                }

                // Hover pill background for inactive tabs
                for (int i = 0; i < catLabels.Length; i++)
                {
                    RectangleF tabRect = SettingsCategoryTabRects[i];
                    bool isHov = (SettingsCategoryIndex != i && tabRect.Contains(LastMouseX, LastMouseY));
                    float h = GetSettingHoverAnim("cat_" + i, isHov);
                    if (h > 0.01f)
                    {
                        using (GraphicsPath tp = CreateRoundedRectanglePath(tabRect, 6f))
                        using (SolidBrush tbg = new SolidBrush(Color.FromArgb((int)Math.Round(180 * h), 226, 232, 240)))
                        {
                            g.FillPath(tbg, tp);
                        }
                    }
                }

                // Sliding active tab pill
                if (_catPillCurX >= 0f && _catPillCurW > 0f)
                {
                    RectangleF activePill = new RectangleF(_catPillCurX, stripRect.Y + pad, _catPillCurW, stripRect.Height - pad * 2f);
                    using (GraphicsPath apShadow = CreateRoundedRectanglePath(new RectangleF(activePill.X, activePill.Y + 1f, activePill.Width, activePill.Height), 6f))
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(14, 15, 23, 42)))
                    {
                        g.FillPath(sb, apShadow);
                    }
                    using (GraphicsPath tp = CreateRoundedRectanglePath(activePill, 6f))
                    using (SolidBrush tbg = new SolidBrush(Color.White))
                    using (Pen tpen = new Pen(Color.FromArgb(203, 213, 225), 1f))
                    {
                        g.FillPath(tbg, tp);
                        g.DrawPath(tpen, tp);
                    }
                }

                // Category labels
                for (int i = 0; i < catLabels.Length; i++)
                {
                    RectangleF tabRect = SettingsCategoryTabRects[i];
                    bool isAct = (SettingsCategoryIndex == i);
                    float h = _settingHoverPos.TryGetValue("cat_" + i, out float hv) ? hv : 0f;
                    Color txtCol = isAct
                        ? Color.FromArgb(37, 99, 235)
                        : LerpColor(Color.FromArgb(71, 85, 105), Color.FromArgb(15, 23, 42), h);
                    using (SolidBrush tb = new SolidBrush(txtCol))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                        g.DrawString(catLabels[i], isAct ? pillBoldFont : pillFont, tb, tabRect, sf);
                    }
                }
            }

            using (Pen sepPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.DrawLine(sepPen, pageX, 118f, pageX + pageW, 118f);
            }
        }

        private void DrawSettingsDashboard(Graphics g, float pageX, float pageW, float startY)
        {
            ResetSettingsHitRects();
            for (int t = 0; t < _settingShortcutTileRects.Length; t++) _settingShortcutTileRects[t] = RectangleF.Empty;

            bool twoCol = (SettingsCategoryIndex == 0 && pageW >= 880f);
            float gap = 18f;
            float colW = twoCol ? (pageW - gap) / 2f : Math.Min(pageW, 820f);
            float leftX = twoCol ? pageX : pageX + (pageW - colW) / 2f;
            float rightX = leftX + colW + gap;

            using (Font secFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (Font secSubFont = new Font("Segoe UI", 8.1f, FontStyle.Regular))
            using (Font rTitleFont = new Font("Segoe UI", 9.3f, FontStyle.Bold))
            using (Font rSubFont = new Font("Segoe UI", 8.0f, FontStyle.Regular))
            using (Font ctrlFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (SolidBrush secBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (SolidBrush mutedBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            using (Pen sepPen = new Pen(Color.FromArgb(241, 245, 249), 1f))
            {
                float maxBottomY = startY;

                if (SettingsCategoryIndex == 0 && twoCol)
                {
                    float ly = startY;
                    ly = DrawSettingsAiCard(g, leftX, ly, colW, secFont, secSubFont, rTitleFont, rSubFont, ctrlFont, secBrush, mutedBrush, sepPen) + gap;
                    ly = DrawSettingsAppearanceCard(g, leftX, ly, colW, secFont, secSubFont, rTitleFont, rSubFont, ctrlFont, secBrush, mutedBrush, sepPen) + gap;
                    ly = DrawSettingsShortcutsCard(g, leftX, ly, colW, secFont, secSubFont, rTitleFont, secBrush, mutedBrush);

                    float ry = startY;
                    ry = DrawSettingsSystemCard(g, rightX, ry, colW, secFont, secSubFont, rTitleFont, rSubFont, ctrlFont, secBrush, mutedBrush, sepPen) + gap;
                    ry = DrawSettingsDefaultAppsCard(g, rightX, ry, colW, secFont, secSubFont, rTitleFont, rSubFont, ctrlFont, secBrush, mutedBrush, sepPen);

                    float aboutY = Math.Max(ly, ry) + gap;
                    maxBottomY = DrawSettingsAboutCard(g, pageX, aboutY, pageW, rTitleFont, rSubFont, ctrlFont, secBrush, mutedBrush);
                }
                else
                {
                    float y = startY;
                    if (SettingsCategoryIndex == 0 || SettingsCategoryIndex == 1)
                        y = DrawSettingsAiCard(g, leftX, y, colW, secFont, secSubFont, rTitleFont, rSubFont, ctrlFont, secBrush, mutedBrush, sepPen) + gap;
                    if (SettingsCategoryIndex == 0 || SettingsCategoryIndex == 2)
                        y = DrawSettingsAppearanceCard(g, leftX, y, colW, secFont, secSubFont, rTitleFont, rSubFont, ctrlFont, secBrush, mutedBrush, sepPen) + gap;
                    if (SettingsCategoryIndex == 0 || SettingsCategoryIndex == 3)
                        y = DrawSettingsSystemCard(g, leftX, y, colW, secFont, secSubFont, rTitleFont, rSubFont, ctrlFont, secBrush, mutedBrush, sepPen) + gap;
                    if (SettingsCategoryIndex == 0 || SettingsCategoryIndex == 4)
                    {
                        y = DrawSettingsShortcutsCard(g, leftX, y, colW, secFont, secSubFont, rTitleFont, secBrush, mutedBrush) + gap;
                        y = DrawSettingsDefaultAppsCard(g, leftX, y, colW, secFont, secSubFont, rTitleFont, rSubFont, ctrlFont, secBrush, mutedBrush, sepPen) + gap;
                    }
                    if (SettingsCategoryIndex == 0 || SettingsCategoryIndex == 1)
                        y = DrawSettingsAboutCard(g, leftX, y, colW, rTitleFont, rSubFont, ctrlFont, secBrush, mutedBrush) + gap;

                    maxBottomY = y - gap;
                }

                _settingsContentTotalH = Math.Max(100f, (maxBottomY - startY) + 28f);
            }
        }

        private float DrawSettingsAiCard(Graphics g, float x, float y, float w, Font secFont, Font secSubFont, Font rTitleFont, Font rSubFont, Font ctrlFont, SolidBrush titleBrush, SolidBrush mutedBrush, Pen sepPen)
        {
            float innerW = w - 36f;
            bool oneRowModels = (innerW >= 450f);
            float aiModelRowH = oneRowModels ? 92f : 128f;
            float cardH = 58f + 56f + aiModelRowH + 56f;
            RectangleF card = new RectangleF(x, y, w, cardH);
            DrawSettingsCardShell(g, card);
            DrawSettingsCardHeader(g, card, AppL10n.T("Trí tuệ nhân tạo", "Artificial Intelligence"), AppL10n.T("Trợ lý AI Copilot và cấu hình mô hình", "AI Copilot assistant & model selection"), Color.FromArgb(238, 242, 255), Color.FromArgb(79, 70, 229), (gr, ix, iy, isz, icol) => DrawVectorSparkleIcon(gr, ix, iy, (int)isz, icol), secFont, secSubFont, titleBrush, mutedBrush);

            float ry = card.Y + 58f;
            DrawToggleSettingRow(g, card.X, ry, card.Width, 56f, AppL10n.T("Trợ lý AI Copilot", "AI Copilot Assistant"), AppL10n.T("Hỗ trợ viết, tóm tắt và phân tích dữ liệu", "Smart writing, summaries & data insights"), AppSettings.Instance.AiCopilotEnabled, "tog_ai_copilot", out SettingAiCopilotToggleRect, rTitleFont, rSubFont, titleBrush, mutedBrush);
            g.DrawLine(sepPen, card.X + 18f, ry + 56f, card.Right - 18f, ry + 56f);
            ry += 56f;

            // Row 2: AI Model selector grid with gliding active pill
            g.DrawString(AppL10n.T("Mô hình AI mặc định", "Default AI Model"), rTitleFont, titleBrush, card.X + 18f, ry + 11f);
            g.DrawString(AppL10n.T("Chọn nhà cung cấp xử lý yêu cầu Copilot", "Choose provider for Copilot requests"), rSubFont, mutedBrush, card.X + 18f, ry + 29f);

            int prov = AppSettings.Instance.SelectedAiProvider;
            float mx = card.X + 18f;
            float my = ry + 50f;
            float mGap = 8f;
            if (oneRowModels)
            {
                float mw = (innerW - mGap * 3f) / 4f;
                SettingAiProviderGoogleRect = new RectangleF(mx, my, mw, 32f);
                SettingAiProviderOpenAiRect = new RectangleF(mx + mw + mGap, my, mw, 32f);
                SettingAiProviderClaudeRect = new RectangleF(mx + (mw + mGap) * 2f, my, mw, 32f);
                SettingAiProviderOllamaRect = new RectangleF(mx + (mw + mGap) * 3f, my, mw, 32f);
            }
            else
            {
                float mw = (innerW - mGap) / 2f;
                SettingAiProviderGoogleRect = new RectangleF(mx, my, mw, 32f);
                SettingAiProviderOpenAiRect = new RectangleF(mx + mw + mGap, my, mw, 32f);
                SettingAiProviderClaudeRect = new RectangleF(mx, my + 38f, mw, 32f);
                SettingAiProviderOllamaRect = new RectangleF(mx + mw + mGap, my + 38f, mw, 32f);
            }

            RectangleF activeAiRect = prov == 1 ? SettingAiProviderOpenAiRect : (prov == 2 ? SettingAiProviderClaudeRect : (prov == 3 ? SettingAiProviderOllamaRect : SettingAiProviderGoogleRect));
            _aiModelTargetX = activeAiRect.X - card.X;
            _aiModelTargetY = activeAiRect.Y - card.Y;
            _aiModelTargetW = activeAiRect.Width;
            if (_aiModelCurX < 0f || _aiModelCurW < 0f)
            {
                _aiModelCurX = _aiModelTargetX;
                _aiModelCurY = _aiModelTargetY;
                _aiModelCurW = _aiModelTargetW;
            }

            DrawWpsSegmentedButton(g, SettingAiProviderGoogleRect, "Google Gemini", prov == 0, ctrlFont, "seg_ai_0", suppressActiveBox: true);
            DrawWpsSegmentedButton(g, SettingAiProviderOpenAiRect, "OpenAI GPT", prov == 1, ctrlFont, "seg_ai_1", suppressActiveBox: true);
            DrawWpsSegmentedButton(g, SettingAiProviderClaudeRect, "Claude 3.5", prov == 2, ctrlFont, "seg_ai_2", suppressActiveBox: true);
            DrawWpsSegmentedButton(g, SettingAiProviderOllamaRect, "Ollama Local", prov == 3, ctrlFont, "seg_ai_3", suppressActiveBox: true);

            // Render gliding selection frame & subtle tint over the AI buttons (relative to card origin)
            RectangleF slidingAi = new RectangleF(card.X + _aiModelCurX, card.Y + _aiModelCurY, _aiModelCurW, 32f);
            using (GraphicsPath ap = CreateRoundedRectanglePath(slidingAi, 6f))
            using (SolidBrush abg = new SolidBrush(Color.FromArgb(46, 37, 99, 235)))
            using (Pen apen = new Pen(Color.FromArgb(37, 99, 235), 1.4f))
            {
                g.FillPath(abg, ap);
                g.DrawPath(apen, ap);
            }

            g.DrawLine(sepPen, card.X + 18f, ry + aiModelRowH, card.Right - 18f, ry + aiModelRowH);
            ry += aiModelRowH;

            // Row 3: API Key button
            SettingAiConfigBtnRect = new RectangleF(card.Right - 180f, ry + 11f, 162f, 34f);
            DrawSettingRowLabel(g, card.X + 18f, ry, card.Width - 208f, 56f, AppL10n.T("Cấu hình API Key", "API Key Configuration"), AppL10n.T("Quản lý khóa API cho các mô hình AI", "Manage personal API keys"), rTitleFont, rSubFont, titleBrush, mutedBrush);
            DrawWpsActionButton(g, SettingAiConfigBtnRect, AppL10n.T("Thiết lập API Key...", "Configure API Key..."), Color.FromArgb(79, 70, 229), ctrlFont, '\uE8D7', Color.FromArgb(238, 242, 255), Color.FromArgb(199, 210, 254), "btn_ai_cfg");
            return card.Bottom;
        }

        private float DrawSettingsAppearanceCard(Graphics g, float x, float y, float w, Font secFont, Font secSubFont, Font rTitleFont, Font rSubFont, Font ctrlFont, SolidBrush titleBrush, SolidBrush mutedBrush, Pen sepPen)
        {
            float cardH = 58f + 58f * 3f;
            RectangleF card = new RectangleF(x, y, w, cardH);
            DrawSettingsCardShell(g, card);
            DrawSettingsCardHeader(g, card, AppL10n.T("Giao diện & Ngôn ngữ", "Appearance & Language"), AppL10n.T("Thanh công cụ, chủ đề màu và ngôn ngữ hiển thị", "Ribbon layout, color theme & UI language"), Color.FromArgb(239, 246, 255), Color.FromArgb(37, 99, 235), (gr, ix, iy, isz, icol) => DrawFluentPaletteIcon(gr, ix, iy, isz, icol), secFont, secSubFont, titleBrush, mutedBrush);

            float ry = card.Y + 58f;
            float segTrayW = Math.Min(232f, Math.Max(184f, w * 0.44f));
            float segBtnW = (segTrayW - 10f) / 2f;
            float trayX = card.Right - 18f - segTrayW;

            // Row 1: Ribbon Style
            DrawSettingRowLabel(g, card.X + 18f, ry, trayX - card.X - 28f, 58f, AppL10n.T("Kiểu thanh công cụ", "Ribbon Style"), AppL10n.T("Dạng Đảo nổi hoặc Cổ điển", "Floating Island or Classic bar"), rTitleFont, rSubFont, titleBrush, mutedBrush);
            bool isIsland = (AppSettings.Instance.RibbonStyle == RibbonStyle.Island);
            SettingRibbonIslandRect = new RectangleF(trayX + 3f, ry + 15f, segBtnW, 28f);
            SettingRibbonStandardRect = new RectangleF(trayX + 7f + segBtnW, ry + 15f, segBtnW, 28f);
            DrawAnimatedSegmentedTray(g, "tray_ribbon", new RectangleF(trayX, ry + 12f, segTrayW, 34f), isIsland ? SettingRibbonIslandRect : SettingRibbonStandardRect);
            DrawWpsSegmentedButton(g, SettingRibbonIslandRect, AppL10n.T("Dạng Đảo", "Island"), isIsland, ctrlFont, "seg_rib_0", suppressActiveBox: true, insideTray: true);
            DrawWpsSegmentedButton(g, SettingRibbonStandardRect, AppL10n.T("Tiêu chuẩn", "Classic"), !isIsland, ctrlFont, "seg_rib_1", suppressActiveBox: true, insideTray: true);
            g.DrawLine(sepPen, card.X + 18f, ry + 58f, card.Right - 18f, ry + 58f);
            ry += 58f;

            // Row 2: Color Theme
            DrawSettingRowLabel(g, card.X + 18f, ry, trayX - card.X - 28f, 58f, AppL10n.T("Chủ đề giao diện", "Color Theme"), AppL10n.T("Chế độ hiển thị Sáng hoặc Tối", "Light or Dark mode"), rTitleFont, rSubFont, titleBrush, mutedBrush);
            bool isLight = (AppSettings.Instance.Theme == AppTheme.Light);
            SettingThemeLightRect = new RectangleF(trayX + 3f, ry + 15f, segBtnW, 28f);
            SettingThemeDarkRect = new RectangleF(trayX + 7f + segBtnW, ry + 15f, segBtnW, 28f);
            DrawAnimatedSegmentedTray(g, "tray_theme", new RectangleF(trayX, ry + 12f, segTrayW, 34f), isLight ? SettingThemeLightRect : SettingThemeDarkRect);
            DrawWpsSegmentedButton(g, SettingThemeLightRect, AppL10n.T("Sáng (Light)", "Light"), isLight, ctrlFont, "seg_thm_0", suppressActiveBox: true, insideTray: true);
            DrawWpsSegmentedButton(g, SettingThemeDarkRect, AppL10n.T("Tối (Dark)", "Dark"), !isLight, ctrlFont, "seg_thm_1", suppressActiveBox: true, insideTray: true);
            g.DrawLine(sepPen, card.X + 18f, ry + 58f, card.Right - 18f, ry + 58f);
            ry += 58f;

            // Row 3: Display Language
            DrawSettingRowLabel(g, card.X + 18f, ry, trayX - card.X - 28f, 58f, AppL10n.T("Ngôn ngữ giao diện", "Display Language"), AppL10n.T("Chuyển đổi ngay không cần khởi động lại", "Applies live across all screens"), rTitleFont, rSubFont, titleBrush, mutedBrush);
            bool isVi = (AppSettings.Instance.Language == AppLanguage.Vietnamese);
            SettingLangViRect = new RectangleF(trayX + 3f, ry + 15f, segBtnW, 28f);
            SettingLangEnRect = new RectangleF(trayX + 7f + segBtnW, ry + 15f, segBtnW, 28f);
            DrawAnimatedSegmentedTray(g, "tray_lang", new RectangleF(trayX, ry + 12f, segTrayW, 34f), isVi ? SettingLangViRect : SettingLangEnRect);
            DrawWpsSegmentedButton(g, SettingLangViRect, "Tiếng Việt", isVi, ctrlFont, "seg_lng_0", suppressActiveBox: true, insideTray: true);
            DrawWpsSegmentedButton(g, SettingLangEnRect, "English (US)", !isVi, ctrlFont, "seg_lng_1", suppressActiveBox: true, insideTray: true);
            return card.Bottom;
        }

        private float DrawSettingsSystemCard(Graphics g, float x, float y, float w, Font secFont, Font secSubFont, Font rTitleFont, Font rSubFont, Font ctrlFont, SolidBrush titleBrush, SolidBrush mutedBrush, Pen sepPen)
        {
            float cardH = 58f + 56f * 6f;
            RectangleF card = new RectangleF(x, y, w, cardH);
            DrawSettingsCardShell(g, card);
            DrawSettingsCardHeader(g, card, AppL10n.T("Hệ thống & Soạn thảo", "System & Editing"), AppL10n.T("Sao lưu tự động, hiệu năng cuộn và gõ thông minh", "AutoSave, scrolling smoothness & typing aids"), Color.FromArgb(236, 253, 245), Color.FromArgb(5, 150, 105), (gr, ix, iy, isz, icol) => DrawFluentSlidersIcon(gr, ix, iy, isz, icol), secFont, secSubFont, titleBrush, mutedBrush);

            float ry = card.Y + 58f;
            DrawToggleSettingRow(g, card.X, ry, card.Width, 56f, AppL10n.T("Tự động lưu", "AutoSave"), AppL10n.T("Lưu bản nháp định kỳ tránh mất dữ liệu", "Periodically save drafts automatically"), AppSettings.Instance.AutoSaveEnabled, "tog_autosave", out SettingAutoSaveToggleRect, rTitleFont, rSubFont, titleBrush, mutedBrush);
            g.DrawLine(sepPen, card.X + 18f, ry + 56f, card.Right - 18f, ry + 56f);
            ry += 56f;

            // Save Interval segmented tray with gliding active pill
            float intTrayW = 196f;
            float intTrayX = card.Right - 18f - intTrayW;
            DrawSettingRowLabel(g, card.X + 18f, ry, intTrayX - card.X - 28f, 56f, AppL10n.T("Tần suất lưu", "Save Interval"), AppL10n.T("Khoảng thời gian giữa các lần lưu", "Interval between automatic saves"), rTitleFont, rSubFont, titleBrush, mutedBrush);
            int interval = AppSettings.Instance.AutoSaveIntervalSeconds;
            float ibw = (intTrayW - 14f) / 3f;
            SettingAutoSave30Rect = new RectangleF(intTrayX + 3f, ry + 14f, ibw, 28f);
            SettingAutoSave60Rect = new RectangleF(intTrayX + 7f + ibw, ry + 14f, ibw, 28f);
            SettingAutoSave120Rect = new RectangleF(intTrayX + 11f + ibw * 2f, ry + 14f, ibw, 28f);
            RectangleF activeIntRect = interval == 30 ? SettingAutoSave30Rect : (interval == 120 ? SettingAutoSave120Rect : SettingAutoSave60Rect);
            DrawAnimatedSegmentedTray(g, "tray_interval", new RectangleF(intTrayX, ry + 11f, intTrayW, 34f), activeIntRect);
            DrawWpsSegmentedButton(g, SettingAutoSave30Rect, "30s", interval == 30, ctrlFont, "seg_int_30", suppressActiveBox: true, insideTray: true);
            DrawWpsSegmentedButton(g, SettingAutoSave60Rect, "60s", interval == 60, ctrlFont, "seg_int_60", suppressActiveBox: true, insideTray: true);
            DrawWpsSegmentedButton(g, SettingAutoSave120Rect, "120s", interval == 120, ctrlFont, "seg_int_120", suppressActiveBox: true, insideTray: true);
            g.DrawLine(sepPen, card.X + 18f, ry + 56f, card.Right - 18f, ry + 56f);
            ry += 56f;

            DrawToggleSettingRow(g, card.X, ry, card.Width, 56f, AppL10n.T("Cuộn trang mượt mà", "Smooth Scrolling"), AppL10n.T("Giảm khựng hình khi cuộn tài liệu dài", "Silky scrolling across long documents"), AppSettings.Instance.EnableSmoothScroll, "tog_smooth_scroll", out SettingSmoothScrollRect, rTitleFont, rSubFont, titleBrush, mutedBrush);
            g.DrawLine(sepPen, card.X + 18f, ry + 56f, card.Right - 18f, ry + 56f);
            ry += 56f;

            DrawToggleSettingRow(g, card.X, ry, card.Width, 56f, AppL10n.T("Thanh thước đo", "Show Rulers"), AppL10n.T("Hiển thị thước căn lề trên trang soạn thảo", "Display margin rulers in Writer"), AppSettings.Instance.ShowRuler, "tog_ruler", out SettingRulerRect, rTitleFont, rSubFont, titleBrush, mutedBrush);
            g.DrawLine(sepPen, card.X + 18f, ry + 56f, card.Right - 18f, ry + 56f);
            ry += 56f;

            DrawToggleSettingRow(g, card.X, ry, card.Width, 56f, AppL10n.T("Tự động sửa lỗi gõ", "AutoCorrect Typing"), AppL10n.T("Chuẩn hóa dấu câu và viết hoa tự động", "Fix casing and punctuation automatically"), AppSettings.Instance.AutoCorrectVietnamese, "tog_autocorrect", out SettingAutoCorrectViRect, rTitleFont, rSubFont, titleBrush, mutedBrush);
            g.DrawLine(sepPen, card.X + 18f, ry + 56f, card.Right - 18f, ry + 56f);
            ry += 56f;

            DrawToggleSettingRow(g, card.X, ry, card.Width, 56f, AppL10n.T("Gợi ý từ thông minh", "Smart Word Prediction"), AppL10n.T("Hiển thị từ gợi ý nhanh khi gõ văn bản", "Suggest completions inline while typing"), AppSettings.Instance.WordPredictionEnabled, "tog_wordpred", out SettingWordPredictionRect, rTitleFont, rSubFont, titleBrush, mutedBrush);
            return card.Bottom;
        }

        private float DrawSettingsShortcutsCard(Graphics g, float x, float y, float w, Font secFont, Font secSubFont, Font rTitleFont, SolidBrush titleBrush, SolidBrush mutedBrush)
        {
            float innerW = w - 36f;
            bool grid2x2 = (innerW >= 360f);
            float cardH = 58f + (grid2x2 ? 148f : 224f);
            RectangleF card = new RectangleF(x, y, w, cardH);
            DrawSettingsCardShell(g, card);
            DrawSettingsCardHeader(g, card, AppL10n.T("Lối tắt màn hình", "Desktop Shortcuts"), AppL10n.T("Tạo biểu tượng truy cập nhanh trên Desktop", "Pin standalone app icons to your Desktop"), Color.FromArgb(224, 242, 254), Color.FromArgb(2, 132, 199), (gr, ix, iy, isz, icol) => DrawVectorDesktopIcon(gr, ix, iy, isz, icol), secFont, secSubFont, titleBrush, mutedBrush);

            float bodyY = card.Y + 66f;
            float tileGap = 10f;
            bool hasWriterSc = DesktopShortcutManager.IsShortcutExists("ZenQ Writer") || DesktopShortcutManager.IsShortcutExists("GenOffice Writer");
            bool hasCalcSc = DesktopShortcutManager.IsShortcutExists("ZenQ Calc") || DesktopShortcutManager.IsShortcutExists("GenOffice Calc");
            bool hasSlidesSc = DesktopShortcutManager.IsShortcutExists("ZenQ Slides") || DesktopShortcutManager.IsShortcutExists("GenOffice Slides");
            bool hasPdfSc = DesktopShortcutManager.IsShortcutExists("ZenQ PDF") || DesktopShortcutManager.IsShortcutExists("GenOffice PDF");

            if (grid2x2)
            {
                float tw = (innerW - tileGap) / 2f;
                float th = 58f;
                DrawShortcutTile(g, 0, new RectangleF(card.X + 18f, bodyY, tw, th), "ZenQ Writer", OfficeAppMode.Writer, hasWriterSc, "tog_sc_writer", out SettingShortcutWriterRect, rTitleFont, titleBrush);
                DrawShortcutTile(g, 1, new RectangleF(card.X + 18f + tw + tileGap, bodyY, tw, th), "ZenQ Calc", OfficeAppMode.Calc, hasCalcSc, "tog_sc_calc", out SettingShortcutCalcRect, rTitleFont, titleBrush);
                DrawShortcutTile(g, 2, new RectangleF(card.X + 18f, bodyY + th + tileGap, tw, th), "ZenQ Slides", OfficeAppMode.Slides, hasSlidesSc, "tog_sc_slides", out SettingShortcutSlidesRect, rTitleFont, titleBrush);
                DrawShortcutTile(g, 3, new RectangleF(card.X + 18f + tw + tileGap, bodyY + th + tileGap, tw, th), "ZenQ PDF", OfficeAppMode.Pdf, hasPdfSc, "tog_sc_pdf", out SettingShortcutPdfRect, rTitleFont, titleBrush);
            }
            else
            {
                float th = 48f;
                DrawShortcutTile(g, 0, new RectangleF(card.X + 18f, bodyY, innerW, th), "ZenQ Writer", OfficeAppMode.Writer, hasWriterSc, "tog_sc_writer", out SettingShortcutWriterRect, rTitleFont, titleBrush);
                DrawShortcutTile(g, 1, new RectangleF(card.X + 18f, bodyY + 54f, innerW, th), "ZenQ Calc", OfficeAppMode.Calc, hasCalcSc, "tog_sc_calc", out SettingShortcutCalcRect, rTitleFont, titleBrush);
                DrawShortcutTile(g, 2, new RectangleF(card.X + 18f, bodyY + 108f, innerW, th), "ZenQ Slides", OfficeAppMode.Slides, hasSlidesSc, "tog_sc_slides", out SettingShortcutSlidesRect, rTitleFont, titleBrush);
                DrawShortcutTile(g, 3, new RectangleF(card.X + 18f, bodyY + 162f, innerW, th), "ZenQ PDF", OfficeAppMode.Pdf, hasPdfSc, "tog_sc_pdf", out SettingShortcutPdfRect, rTitleFont, titleBrush);
            }
            return card.Bottom;
        }

        private float DrawSettingsDefaultAppsCard(Graphics g, float x, float y, float w, Font secFont, Font secSubFont, Font rTitleFont, Font rSubFont, Font ctrlFont, SolidBrush titleBrush, SolidBrush mutedBrush, Pen sepPen)
        {
            float cardH = 58f + 56f * 4f + 62f;
            RectangleF card = new RectangleF(x, y, w, cardH);
            DrawSettingsCardShell(g, card);
            DrawSettingsCardHeader(g, card, AppL10n.T("Ứng dụng mặc định", "Default Apps"), AppL10n.T("Mở trực tiếp .docx, .xlsx, .pptx, .pdf bằng ZenQ Office", "Open Office documents & PDFs with ZenQ Office"), Color.FromArgb(204, 251, 241), Color.FromArgb(13, 148, 136), (gr, ix, iy, isz, icol) => DrawVectorDefaultAppIcon(gr, ix, iy, isz, icol), secFont, secSubFont, titleBrush, mutedBrush);

            float ry = card.Y + 58f;
            DrawDefaultAppRow(g, card.X, ry, card.Width, 56f, "ZenQ Writer", ".docx, .doc, .rtf, .txt, .dotx", OfficeAppMode.Writer, FileAssociationManager.IsAppAssociated("-word"), "tog_def_writer", out SettingAssocWriterRect, rTitleFont, rSubFont, titleBrush, mutedBrush);
            g.DrawLine(sepPen, card.X + 18f, ry + 56f, card.Right - 18f, ry + 56f);
            ry += 56f;

            DrawDefaultAppRow(g, card.X, ry, card.Width, 56f, "ZenQ Calc", ".xlsx, .xls, .csv, .tsv, .ods", OfficeAppMode.Calc, FileAssociationManager.IsAppAssociated("-calc"), "tog_def_calc", out SettingAssocCalcRect, rTitleFont, rSubFont, titleBrush, mutedBrush);
            g.DrawLine(sepPen, card.X + 18f, ry + 56f, card.Right - 18f, ry + 56f);
            ry += 56f;

            DrawDefaultAppRow(g, card.X, ry, card.Width, 56f, "ZenQ Slides", ".pptx, .ppt, .ppsx, .potx", OfficeAppMode.Slides, FileAssociationManager.IsAppAssociated("-slides"), "tog_def_slides", out SettingAssocSlidesRect, rTitleFont, rSubFont, titleBrush, mutedBrush);
            g.DrawLine(sepPen, card.X + 18f, ry + 56f, card.Right - 18f, ry + 56f);
            ry += 56f;

            bool isPdfDef = FileAssociationManager.IsAppAssociated("-pdf");
            string pdfDesc = isPdfDef
                ? AppL10n.T("Đã đặt làm trình xem mặc định (.pdf)", "Active default PDF viewer (.pdf)")
                : AppL10n.T(".pdf (Nhấn công tắc để đặt mặc định)", ".pdf (Toggle to set as default)");
            DrawDefaultAppRow(g, card.X, ry, card.Width, 56f, "ZenQ PDF", pdfDesc, OfficeAppMode.Pdf, isPdfDef, "tog_def_pdf", out SettingAssocPdfRect, rTitleFont, rSubFont, titleBrush, isPdfDef ? titleBrush : mutedBrush);
            ry += 56f;

            RectangleF footerRect = new RectangleF(card.X + 1f, ry, card.Width - 2f, 61f);
            using (GraphicsPath fp = CreateRoundedRectanglePath(new RectangleF(card.X + 1f, ry - 8f, card.Width - 2f, 69f), 11f))
            using (SolidBrush fbg = new SolidBrush(Color.FromArgb(248, 250, 252)))
            {
                GraphicsState fgs = g.Save();
                g.SetClip(footerRect);
                g.FillPath(fbg, fp);
                g.Restore(fgs);
            }
            g.DrawLine(sepPen, card.X + 1f, ry, card.Right - 1f, ry);

            float btnGap = 10f;
            float btnAreaW = card.Width - 36f;
            float bw = (btnAreaW - btnGap * 2f) / 3f;
            float bx = card.X + 18f;
            float by = ry + 14f;
            SettingAssocSetAllBtnRect = new RectangleF(bx, by, bw, 33f);
            SettingAssocUnsetAllBtnRect = new RectangleF(bx + bw + btnGap, by, bw, 33f);
            SettingAssocWinSettingsRect = new RectangleF(bx + (bw + btnGap) * 2f, by, bw, 33f);

            DrawWpsActionButton(g, SettingAssocSetAllBtnRect, AppL10n.T("Đặt tất cả", "Set All"), Color.FromArgb(4, 120, 87), ctrlFont, '\uE73E', Color.FromArgb(236, 253, 245), Color.FromArgb(167, 243, 208), "btn_assoc_setall");
            DrawWpsActionButton(g, SettingAssocUnsetAllBtnRect, AppL10n.T("Tắt tất cả", "Clear All"), Color.FromArgb(71, 85, 105), ctrlFont, '\uE711', Color.White, Color.FromArgb(203, 213, 225), "btn_assoc_unsetall");
            DrawWpsActionButton(g, SettingAssocWinSettingsRect, AppL10n.T("Mở Windows...", "Windows Apps..."), Color.FromArgb(29, 78, 216), ctrlFont, '\uE713', Color.FromArgb(239, 246, 255), Color.FromArgb(191, 219, 254), "btn_assoc_win");
            return card.Bottom;
        }

        private float DrawSettingsAboutCard(Graphics g, float x, float y, float w, Font rTitleFont, Font rSubFont, Font ctrlFont, SolidBrush titleBrush, SolidBrush mutedBrush)
        {
            bool wide = (w >= 640f);
            float cardH = wide ? 84f : 118f;
            RectangleF card = new RectangleF(x, y, w, cardH);
            DrawSettingsCardShell(g, card);

            DrawVectorAppLogoIcon(g, card.X + 18f, card.Y + 20f, 44f);
            g.DrawString("ZENQ OFFICE Professional", rTitleFont, titleBrush, card.X + 74f, card.Y + 22f);
            g.DrawString(AppL10n.T("Bộ ứng dụng văn phòng đa năng • Tài liệu, Bảng tính, Trình chiếu, PDF & Đồ họa", "All-in-one Desktop Productivity Suite • Documents, Sheets, Slides, PDF & Design"), rSubFont, mutedBrush, card.X + 74f, card.Y + 42f);

            if (wide)
            {
                SettingCheckUpdateBtnRect = new RectangleF(card.Right - 306f, card.Y + 25f, 150f, 34f);
                SettingHelpBtnRect = new RectangleF(card.Right - 146f, card.Y + 25f, 128f, 34f);
            }
            else
            {
                float bw = (w - 46f) / 2f;
                SettingCheckUpdateBtnRect = new RectangleF(card.X + 18f, card.Y + 72f, bw, 34f);
                SettingHelpBtnRect = new RectangleF(card.X + 28f + bw, card.Y + 72f, bw, 34f);
            }
            DrawWpsActionButton(g, SettingCheckUpdateBtnRect, AppL10n.T("Kiểm tra cập nhật", "Check Updates"), Color.FromArgb(29, 78, 216), ctrlFont, '\uE72C', Color.FromArgb(239, 246, 255), Color.FromArgb(191, 219, 254), "btn_about_upd");
            DrawWpsActionButton(g, SettingHelpBtnRect, AppL10n.T("Xem tài liệu", "User Guide"), Color.FromArgb(71, 85, 105), ctrlFont, '\uE897', Color.White, Color.FromArgb(203, 213, 225), "btn_about_help");
            return card.Bottom;
        }

        private void DrawSettingsCardShell(Graphics g, RectangleF rect)
        {
            RectangleF shadowRect = new RectangleF(rect.X, rect.Y + 2f, rect.Width, rect.Height);
            using (GraphicsPath sp = CreateRoundedRectanglePath(shadowRect, 12f))
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(10, 15, 23, 42)))
            {
                g.FillPath(sb, sp);
            }
            using (GraphicsPath p = CreateRoundedRectanglePath(rect, 12f))
            using (SolidBrush bg = new SolidBrush(Color.White))
            using (Pen border = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.FillPath(bg, p);
                g.DrawPath(border, p);
            }
        }

        private void DrawSettingsCardHeader(Graphics g, RectangleF card, string title, string subtitle, Color badgeBg, Color iconColor, Action<Graphics, float, float, float, Color> drawIcon, Font titleFont, Font subFont, SolidBrush titleBrush, SolidBrush mutedBrush)
        {
            RectangleF iconBox = new RectangleF(card.X + 16f, card.Y + 13f, 32f, 32f);
            using (GraphicsPath ip = CreateRoundedRectanglePath(iconBox, 8f))
            using (SolidBrush ib = new SolidBrush(badgeBg))
            {
                g.FillPath(ib, ip);
            }
            drawIcon(g, iconBox.X + 8f, iconBox.Y + 8f, 16f, iconColor);

            float textMaxW = Math.Max(100f, card.Width - 72f);
            StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            g.DrawString(title, titleFont, titleBrush, new RectangleF(card.X + 58f, card.Y + 11f, textMaxW, 20f), sf);
            g.DrawString(subtitle, subFont, mutedBrush, new RectangleF(card.X + 58f, card.Y + 31f, textMaxW, 18f), sf);
            using (Pen hPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.DrawLine(hPen, card.X + 1f, card.Y + 57f, card.Right - 1f, card.Y + 57f);
            }
        }

        private void DrawSettingRowLabel(Graphics g, float x, float y, float maxW, float rowH, string title, string subtitle, Font titleFont, Font subFont, SolidBrush titleBrush, SolidBrush subBrush)
        {
            StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            float safeW = Math.Max(80f, maxW);
            g.DrawString(title, titleFont, titleBrush, new RectangleF(x, y + 10f, safeW, 20f), sf);
            g.DrawString(subtitle, subFont, subBrush, new RectangleF(x, y + 29f, safeW, 18f), sf);
        }

        private void DrawToggleSettingRow(Graphics g, float cardX, float y, float cardW, float rowH, string title, string subtitle, bool isOn, string animKey, out RectangleF hitRect, Font titleFont, Font subFont, SolidBrush titleBrush, SolidBrush mutedBrush)
        {
            hitRect = new RectangleF(cardX + cardW - 64f, y + (rowH - 28f) / 2f, 46f, 28f);
            DrawSettingRowLabel(g, cardX + 18f, y, cardW - 96f, rowH, title, subtitle, titleFont, subFont, titleBrush, mutedBrush);
            DrawWpsToggleSwitch(g, hitRect, isOn, animKey);
        }

        private void DrawShortcutTile(Graphics g, int tileIdx, RectangleF tileRect, string appName, OfficeAppMode mode, bool isOn, string animKey, out RectangleF hitRect, Font titleFont, SolidBrush titleBrush)
        {
            if (tileIdx >= 0 && tileIdx < _settingShortcutTileRects.Length) _settingShortcutTileRects[tileIdx] = tileRect;
            hitRect = new RectangleF(tileRect.Right - 56f, tileRect.Y + (tileRect.Height - 28f) / 2f, 44f, 28f);
            bool hovered = tileRect.Contains(LastMouseX, LastMouseY);
            float h = GetSettingHoverAnim("tile_" + animKey, hovered);
            float tOn = GetSettingToggleAnim(animKey, isOn);
            Color bgCol = LerpColor(Color.FromArgb(248, 250, 252), Color.FromArgb(239, 246, 255), h);
            Color bdCol = LerpColor(LerpColor(Color.FromArgb(226, 232, 240), Color.FromArgb(191, 219, 254), tOn), Color.FromArgb(147, 197, 253), h);
            using (GraphicsPath tp = CreateRoundedRectanglePath(tileRect, 9f))
            using (SolidBrush tb = new SolidBrush(bgCol))
            using (Pen tpen = new Pen(bdCol, 1f))
            {
                g.FillPath(tb, tp);
                g.DrawPath(tpen, tp);
            }
            float iconY = tileRect.Y + (tileRect.Height - 28f) / 2f;
            AppIconRenderer.DrawAppIcon(g, tileRect.X + 12f, iconY, 28f, mode);
            StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            g.DrawString(appName, titleFont, titleBrush, new RectangleF(tileRect.X + 48f, tileRect.Y, Math.Max(40f, tileRect.Width - 110f), tileRect.Height), sf);
            DrawWpsToggleSwitch(g, hitRect, isOn, animKey);
        }

        private void DrawDefaultAppRow(Graphics g, float cardX, float y, float cardW, float rowH, string appName, string exts, OfficeAppMode mode, bool isOn, string animKey, out RectangleF hitRect, Font titleFont, Font subFont, SolidBrush titleBrush, SolidBrush subBrush)
        {
            AppIconRenderer.DrawAppIcon(g, cardX + 18f, y + (rowH - 30f) / 2f, 30f, mode);
            hitRect = new RectangleF(cardX + cardW - 64f, y + (rowH - 28f) / 2f, 46f, 28f);
            DrawSettingRowLabel(g, cardX + 58f, y, cardW - 134f, rowH, appName, exts, titleFont, subFont, titleBrush, subBrush);
            DrawWpsToggleSwitch(g, hitRect, isOn, animKey);
        }

        private void DrawAnimatedSegmentedTray(Graphics g, string trayKey, RectangleF trayRect, RectangleF activeBtnRect)
        {
            using (GraphicsPath p = CreateRoundedRectanglePath(trayRect, 7f))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(241, 245, 249)))
            {
                g.FillPath(bg, p);
            }

            float relTargetX = activeBtnRect.X - trayRect.X;
            _settingTrayTargetX[trayKey] = relTargetX;
            _settingTrayTargetW[trayKey] = activeBtnRect.Width;
            if (!_settingTrayCurX.TryGetValue(trayKey, out float curRelX) || !_settingTrayCurW.TryGetValue(trayKey, out float curW))
            {
                curRelX = relTargetX;
                curW = activeBtnRect.Width;
                _settingTrayCurX[trayKey] = curRelX;
                _settingTrayCurW[trayKey] = curW;
            }

            RectangleF pillRect = new RectangleF(trayRect.X + curRelX, activeBtnRect.Y, curW, activeBtnRect.Height);
            using (GraphicsPath sp = CreateRoundedRectanglePath(new RectangleF(pillRect.X, pillRect.Y + 1f, pillRect.Width, pillRect.Height), 5.5f))
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(14, 15, 23, 42)))
            {
                g.FillPath(sb, sp);
            }
            using (GraphicsPath ap = CreateRoundedRectanglePath(pillRect, 5.5f))
            using (SolidBrush abg = new SolidBrush(Color.White))
            using (Pen apen = new Pen(Color.FromArgb(37, 99, 235), 1.25f))
            {
                g.FillPath(abg, ap);
                g.DrawPath(apen, ap);
            }
        }

        private void DrawWpsToggleSwitch(Graphics g, RectangleF rect, bool isOn, string animKey = null)
        {
            bool hovered = rect.Contains(LastMouseX, LastMouseY);
            string hKey = animKey != null ? ("h_" + animKey) : null;
            float h = hKey != null ? GetSettingHoverAnim(hKey, hovered) : (hovered ? 1f : 0f);
            float t = animKey != null ? GetSettingToggleAnim(animKey, isOn) : (isOn ? 1f : 0f);

            float pillW = 42f;
            float pillH = 22f;
            float pillX = rect.Right - pillW;
            float pillY = rect.Y + (rect.Height - pillH) / 2f;
            RectangleF pillRect = new RectangleF(pillX, pillY, pillW, pillH);

            Color baseTrack = LerpColor(Color.FromArgb(203, 213, 225), Color.FromArgb(37, 99, 235), t);
            Color hoverTrack = LerpColor(Color.FromArgb(148, 163, 184), Color.FromArgb(29, 78, 216), t);
            Color pillColor = LerpColor(baseTrack, hoverTrack, h);

            // Soft focus halo on hover
            if (h > 0.02f)
            {
                RectangleF halo = new RectangleF(pillRect.X - 2f, pillRect.Y - 2f, pillRect.Width + 4f, pillRect.Height + 4f);
                using (GraphicsPath hp = CreateRoundedRectanglePath(halo, halo.Height / 2f))
                using (SolidBrush hb = new SolidBrush(Color.FromArgb((int)Math.Round(34 * h), isOn ? Color.FromArgb(37, 99, 235) : Color.FromArgb(100, 116, 139))))
                {
                    g.FillPath(hb, hp);
                }
            }

            using (GraphicsPath p = CreateRoundedRectanglePath(pillRect, pillH / 2f))
            using (SolidBrush bg = new SolidBrush(pillColor))
            {
                g.FillPath(bg, p);
            }

            // Sliding knob with subtle mid-flight micro-stretch
            float thumbDia = 16f;
            float stretch = 3.6f * (float)Math.Sin(t * Math.PI);
            float thumbW = thumbDia + stretch;
            float minX = pillRect.X + 3f;
            float maxX = pillRect.Right - thumbW - 3f;
            float thumbX = minX + (maxX - minX) * t;
            float thumbY = pillY + (pillH - thumbDia) / 2f;
            RectangleF thumbRect = new RectangleF(thumbX, thumbY, thumbW, thumbDia);

            using (GraphicsPath shPath = CreateRoundedRectanglePath(new RectangleF(thumbX + 0.5f, thumbY + 1f, thumbW, thumbDia), thumbDia / 2f))
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(38, 0, 0, 0)))
            {
                g.FillPath(shadow, shPath);
            }
            using (GraphicsPath kPath = CreateRoundedRectanglePath(thumbRect, thumbDia / 2f))
            using (SolidBrush whiteThumb = new SolidBrush(Color.White))
            {
                g.FillPath(whiteThumb, kPath);
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

            ZenQOfficeLogoRenderer.DrawLogo(g, x, y, size, drawBackdrop: true, highDetail: true);

        }



        private void DrawWpsSegmentedButton(Graphics g, RectangleF rect, string label, bool isSelected, Font font, string animKey = null, bool suppressActiveBox = false, bool insideTray = false)
        {
            bool hovered = rect.Contains(LastMouseX, LastMouseY);
            float h = animKey != null ? GetSettingHoverAnim(animKey, hovered) : (hovered ? 1f : 0f);

            if (!insideTray)
            {
                using (GraphicsPath p = CreateRoundedRectanglePath(rect, 6f))
                {
                    if (isSelected && !suppressActiveBox)
                    {
                        using (SolidBrush bg = new SolidBrush(Color.FromArgb(239, 246, 255)))
                        using (Pen pen = new Pen(Color.FromArgb(37, 99, 235), 1.3f))
                        {
                            g.FillPath(bg, p);
                            g.DrawPath(pen, p);
                        }
                    }
                    else
                    {
                        Color bgCol = LerpColor(Color.White, Color.FromArgb(241, 245, 249), h);
                        Color bdCol = LerpColor(Color.FromArgb(226, 232, 240), Color.FromArgb(148, 163, 184), h);
                        using (SolidBrush bg = new SolidBrush(bgCol))
                        using (Pen pen = new Pen(bdCol, 1f))
                        {
                            g.FillPath(bg, p);
                            g.DrawPath(pen, p);
                        }
                    }
                }
            }
            else if (!isSelected && h > 0.01f)
            {
                using (GraphicsPath p = CreateRoundedRectanglePath(rect, 5.5f))
                using (SolidBrush bg = new SolidBrush(Color.FromArgb((int)Math.Round(160 * h), 226, 232, 240)))
                {
                    g.FillPath(bg, p);
                }
            }

            Color txtCol = isSelected
                ? Color.FromArgb(29, 78, 216)
                : LerpColor(Color.FromArgb(71, 85, 105), Color.FromArgb(15, 23, 42), h);
            using (SolidBrush txt = new SolidBrush(txtCol))
            using (Font useFont = isSelected ? new Font(font.FontFamily, font.Size, FontStyle.Bold) : new Font(font.FontFamily, font.Size, FontStyle.Regular))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(label, useFont, txt, rect, sf);
            }
        }

        private void DrawWpsActionButton(Graphics g, RectangleF rect, string label, Color textColor, Font font, char glyph = '\0', Color? fillBg = null, Color? borderColor = null, string animKey = null)
        {
            bool hovered = rect.Contains(LastMouseX, LastMouseY);
            float h = animKey != null ? GetSettingHoverAnim(animKey, hovered) : (hovered ? 1f : 0f);
            float pulse = (animKey != null && _settingPulseKey == animKey) ? _settingPulseVal : 0f;

            RectangleF drawRect = rect;
            if (pulse > 0.02f)
            {
                float inset = 1.2f * pulse;
                drawRect = new RectangleF(rect.X + inset, rect.Y + inset, Math.Max(10f, rect.Width - inset * 2f), Math.Max(10f, rect.Height - inset * 2f));
            }

            Color baseBg = fillBg ?? Color.White;
            Color targetHoverBg = LerpColor(baseBg, textColor, 0.09f);
            Color bgCol = LerpColor(baseBg, targetHoverBg, Math.Min(1f, h + pulse * 0.6f));
            Color baseBd = borderColor ?? Color.FromArgb(203, 213, 225);
            Color bdCol = LerpColor(baseBd, textColor, Math.Min(1f, h * 0.85f + pulse * 0.6f));

            using (GraphicsPath p = CreateRoundedRectanglePath(drawRect, 7f))
            using (SolidBrush bg = new SolidBrush(bgCol))
            using (Pen pen = new Pen(bdCol, 1f))
            {
                g.FillPath(bg, p);
                g.DrawPath(pen, p);
            }

            if (glyph != '\0')
            {
                using (Font drawFont = new Font(font.FontFamily, font.Size, FontStyle.Bold))
                {
                    float textW = g.MeasureString(label, drawFont).Width;
                    float iconSize = 13f;
                    float totalW = iconSize + 5f + textW;
                    float startX = drawRect.X + Math.Max(8f, (drawRect.Width - totalW) / 2f);
                    float iconY = drawRect.Y + (drawRect.Height - iconSize) / 2f;
                    FluentIconRenderer.DrawGlyph(g, glyph, new RectangleF(startX, iconY, iconSize, iconSize), textColor, 11f);
                    using (SolidBrush txt = new SolidBrush(textColor))
                    {
                        RectangleF textRect = new RectangleF(startX + iconSize + 5f, drawRect.Y, Math.Max(20f, drawRect.Right - (startX + iconSize + 5f) - 6f), drawRect.Height);
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                        g.DrawString(label, drawFont, txt, textRect, sf);
                    }
                }
            }
            else
            {
                using (SolidBrush txt = new SolidBrush(textColor))
                using (Font drawFont = new Font(font.FontFamily, font.Size, FontStyle.Bold))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                    g.DrawString(label, drawFont, txt, drawRect, sf);
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

            LastMouseX = x;

            LastMouseY = y;



            bool tmplChanged = false;

            if (ActiveTab == 0 && IsTemplateDiscoveryMode && _templateDiscoveryView != null)

            {

                tmplChanged = _templateDiscoveryView.HandleMouseMove(x, y);

            }

            int prevTab = HoverTabIndex;

            int prevCard = HoverCardIndex;

            int prevRow = HoverRowIndex;

            int prevSettingHover = _hoverSettingsControlId;

            _hoverSettingsControlId = -1;

            bool prevHelp = HoverHelpBtn;

            bool prevUpdate = HoverUpdateBtn;

            bool prevSideDoc = HoverSidebarDoc;

            bool prevSideRec = HoverSidebarRecent;

            bool prevSideStar = HoverSidebarStar;

            int prevSideApp = HoverSidebarApp;

            bool prevToggle = HoverSidebarToggle;

            bool prevPromo = HoverPromoBtn;

            bool prevStorage = HoverStorageManage;

            int prevTmpl = HoverTemplateIndex;

            int prevRowStar = HoverRowStar;

            int prevRowTrash = HoverRowTrash;

            bool prevUpdBtn = HoverRightPanelUpdateBtn;
            bool prevAuthBtn = HoverRightPanelAuthBtn;
            bool prevSignOutBtn = HoverRightPanelSignOutBtn;
            bool prevSharedBtn = HoverRightPanelSharedBtn;
            bool prevSharedHand = (ActiveTab == 4 && _sharedFilesView.IsHoverHand);
            int prevSharedRow = (ActiveTab == 4) ? _sharedFilesView.HoverRowIndex : -1;
            int prevSharedBtnType = (ActiveTab == 4) ? _sharedFilesView.HoverRowBtnType : -1;
            bool prevCompactAvatar = _hoverRightCompactAvatar;
            _hoverRightCompactAvatar = false;
            HoverRightPanelSharedBtn = false;



            HoverTabIndex = -1;

            HoverSidebarDoc = false;

            HoverSidebarRecent = false;

            HoverSidebarStar = false;

            HoverSidebarApp = -1;

            HoverSidebarToggle = false;

            HoverPromoBtn = false;

            HoverStorageManage = false;

            HoverTemplateIndex = -1;

            HoverRowStar = -1;

            HoverRowTrash = -1;

            HoverRightPanelUpdateBtn = false;



            RectangleF myDocsTab = GetMyDocsTabBounds(width, height);

            RectangleF openDocNav = GetOpenDocNavBounds(width, height);

            RectangleF trashTab = GetTrashTabBounds(width, height);

            RectangleF bugReportTab = GetBugReportTabBounds(width, height);

            RectangleF settingsTab = GetSettingsTabBounds(width, height);

            RectangleF helpRect = GetHelpBtnBounds(width, height);

            RectangleF updateRect = GetUpdateBtnBounds(width, height);



            RectangleF tmplNavTab = GetTemplateDiscoveryNavBounds(width, height);

            RectangleF sidebarToggleRect = GetSidebarToggleBounds(width, height);

            if (sidebarToggleRect.Contains(x, y)) HoverSidebarToggle = true;

            else if (myDocsTab.Contains(x, y)) HoverTabIndex = 0;

            else if (tmplNavTab.Contains(x, y)) HoverTabIndex = 10;

            else if (openDocNav.Contains(x, y)) { HoverTabIndex = 3; HoverSidebarDoc = true; }

            else if (trashTab.Contains(x, y)) HoverTabIndex = 1;

            else if (bugReportTab.Contains(x, y)) HoverTabIndex = 4;

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

                HoverRightPanelSharedBtn = GetRightSharedBounds(width).Contains(x, y);

                HoverStorageManage = GetStorageManageBounds(width).Contains(x, y);

                HoverRightPanelUpdateBtn = RightPanelUpdateBtnRect.Contains(x, y);

                HoverRightPanelAuthBtn = !RightPanelAuthBtnRect.IsEmpty && RightPanelAuthBtnRect.Contains(x, y);

                HoverRightPanelSignOutBtn = !RightPanelSignOutBtnRect.IsEmpty && RightPanelSignOutBtnRect.Contains(x, y);
                _hoverRightCompactAvatar = !_compactRightAvatarRect.IsEmpty && _compactRightAvatarRect.Contains(x, y);

                HoverTemplateIndex = -1;

            }



            HoverCardIndex = -1;

            if (ActiveTab == 0 && !IsTemplateDiscoveryMode && y >= 66)

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

            if ((ActiveTab == 0 && !IsTemplateDiscoveryMode) || ActiveTab == 1 || ActiveTab == 3)

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

                    float hitMinY = (ActiveTab == 0 && !IsTemplateDiscoveryMode) ? Math.Max(66f, startY) : listTopY;

                    if (x >= startX && x <= startX + contentW - (maxScroll > 0 ? 14f : 0f) && y >= hitMinY && y <= height - 4f)

                    {

                        float relY = y - startY;

                        if (relY >= 0)

                        {

                            int r = (int)(relY / rowTotalH);

                            float withinRowY = relY % rowTotalH;

                            if (r >= 0 && r < currentList.Count && withinRowY <= rowHeight)

                            {

                                HoverRowIndex = r;

                                if (ActiveTab == 0 || ActiveTab == 3)

                                {

                                    float dotX = startX + contentW - 24f;

                                    if (x >= dotX - 44f && x <= dotX - 18f)

                                    {

                                        HoverRowStar = r;

                                    }

                                    else if (x >= dotX - 16f && x <= dotX + 16f)

                                    {

                                        HoverRowTrash = r;

                                    }

                                }

                            }

                        }

                    }

                }

            }

            else if (ActiveTab == 2)

            {

                for (int ci = 0; ci < SettingsCategoryTabRects.Length; ci++)
                {
                    if (SettingsCategoryTabRects[ci].Contains(x, y))
                    {
                        _hoverSettingsControlId = 1000 + ci;
                        break;
                    }
                }
                if (_hoverSettingsControlId < 0)
                {
                    RectangleF[] settingRects = new RectangleF[]
                    {
                        SettingAiCopilotToggleRect, SettingAiProviderGoogleRect, SettingAiProviderOpenAiRect,
                        SettingAiProviderClaudeRect, SettingAiProviderOllamaRect, SettingAiConfigBtnRect,
                        SettingRibbonIslandRect, SettingRibbonStandardRect, SettingThemeLightRect,
                        SettingThemeDarkRect, SettingAutoSaveToggleRect, SettingAutoSave30Rect,
                        SettingAutoSave60Rect, SettingAutoSave120Rect, SettingSmoothScrollRect,
                        SettingRulerRect, SettingAutoCorrectViRect, SettingWordPredictionRect,
                        SettingLangViRect, SettingLangEnRect, SettingCheckUpdateBtnRect,
                        SettingHelpBtnRect, SettingShortcutWriterRect, SettingShortcutCalcRect,
                        SettingShortcutSlidesRect, SettingShortcutPdfRect, SettingAssocWriterRect,
                        SettingAssocCalcRect, SettingAssocSlidesRect, SettingAssocPdfRect,
                        SettingAssocSetAllBtnRect, SettingAssocUnsetAllBtnRect, SettingAssocWinSettingsRect
                    };
                    for (int si = 0; si < settingRects.Length; si++)
                    {
                        if (settingRects[si].Contains(x, y))
                        {
                            _hoverSettingsControlId = si;
                            break;
                        }
                    }
                    if (_hoverSettingsControlId < 0)
                    {
                        for (int ti = 0; ti < _settingShortcutTileRects.Length; ti++)
                        {
                            if (_settingShortcutTileRects[ti].Contains(x, y))
                            {
                                _hoverSettingsControlId = 500 + ti;
                                break;
                            }
                        }
                    }
                }
                HoverRowIndex = (_hoverSettingsControlId >= 0) ? 200 : -1;

            }



            if (ActiveTab == 4)

            {

                _sharedFilesView.UpdateHover(x, y, SearchKeyword);

            }

            bool sharedChanged = (ActiveTab == 4) &&
                (prevSharedHand != _sharedFilesView.IsHoverHand ||
                 prevSharedRow != _sharedFilesView.HoverRowIndex ||
                 prevSharedBtnType != _sharedFilesView.HoverRowBtnType);

            return (sharedChanged || prevSharedBtn != HoverRightPanelSharedBtn || tmplChanged || prevCard != HoverCardIndex || prevRow != HoverRowIndex || prevSettingHover != _hoverSettingsControlId || prevTab != HoverTabIndex ||

                    prevHelp != HoverHelpBtn || prevUpdate != HoverUpdateBtn || prevSideDoc != HoverSidebarDoc ||

                    prevSideRec != HoverSidebarRecent || prevSideStar != HoverSidebarStar ||

                    prevSideApp != HoverSidebarApp || prevToggle != HoverSidebarToggle || prevPromo != HoverPromoBtn ||

                    prevStorage != HoverStorageManage || prevTmpl != HoverTemplateIndex || prevRowStar != HoverRowStar || prevRowTrash != HoverRowTrash ||

                    prevUpdBtn != HoverRightPanelUpdateBtn || prevAuthBtn != HoverRightPanelAuthBtn ||
                    prevSignOutBtn != HoverRightPanelSignOutBtn || prevCompactAvatar != _hoverRightCompactAvatar);

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



            RectangleF myDocsTab = GetMyDocsTabBounds(width, height);

            RectangleF openDocNav = GetOpenDocNavBounds(width, height);

            RectangleF trashTab = GetTrashTabBounds(width, height);

            RectangleF settingsTab = GetSettingsTabBounds(width, height);

            RectangleF bugReportTab = GetBugReportTabBounds(width, height);

            RectangleF helpRect = GetHelpBtnBounds(width, height);

            RectangleF updateRect = GetUpdateBtnBounds(width, height);

            if (GetSidebarToggleBounds(width, height).Contains(x, y))
            {
                ToggleSidebarCollapsed();
                return null;
            }



            if (ActiveTab == 0 && IsTemplateDiscoveryMode)

            {

                // A. Kiểm tra nhấp vào Sidebar bên trái (x < SidebarWidth)



                if (myDocsTab.Contains(x, y))

                {

                    ActiveTab = 0;

                    IsTemplateDiscoveryMode = false;

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

                    IsTemplateDiscoveryMode = false;

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

                    IsTemplateDiscoveryMode = false;

                    IndicatorTargetX = trashTab.X;

                    IndicatorTargetW = trashTab.Width;

                    ScrollY = 0;

                    return null;

                }

                if (bugReportTab.Contains(x, y))

                {

                    using (var dlg = new BugReportDialogForm())

                    {

                        dlg.ShowDialog();

                    }

                    return null;

                }

                if (settingsTab.Contains(x, y))

                {

                    ActiveTab = 2;

                    IsTemplateDiscoveryMode = false;

                    IndicatorTargetX = settingsTab.X;

                    IndicatorTargetW = settingsTab.Width;

                    ScrollY = 0;

                    return null;

                }

                if (helpRect.Contains(x, y))

                {

                    ShowHelpDialog();

                    return null;

                }

                if (updateRect.Contains(x, y))

                {

                    using (UpdateDialogForm dlg = new UpdateDialogForm(UpdateManager.LastReleaseInfo))

                    {

                        dlg.ShowDialog();

                    }

                    return null;

                }



                // Sidebar App Shortcuts (W / X / P / PDF / Photo)

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



                // B. Kiểm tra nhấp vào Right Panel bên phải (x > width - RightPanelWidth)

                if (GetPromoBtnBounds(width).Contains(x, y))

                {

                    IsTemplateDiscoveryMode = false;

                    TargetScrollY = 0f;

                    SmoothScrollY = 0f;

                    return null;

                }

                if (GetRightSharedBounds(width).Contains(x, y))

                {

                    ActiveTab = 4;

                    IsTemplateDiscoveryMode = false;

                    TargetScrollY = 0f;

                    SmoothScrollY = 0f;

                    _ = CloudShareService.RefreshAndAutoDownloadAsync();

                    return null;

                }

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

                if (RightPanelUpdateBtnRect.Contains(x, y))

                {

                    using (UpdateDialogForm dlg = new UpdateDialogForm(UpdateManager.LastReleaseInfo))

                    {

                        dlg.ShowDialog();

                    }

                    return null;

                }

                if (!RightPanelAuthBtnRect.IsEmpty && RightPanelAuthBtnRect.Contains(x, y))

                {

                    using (AuthDialogForm dlg = new AuthDialogForm())

                    {

                        dlg.ShowDialog();

                    }

                    AppForm.Instance?.Invalidate();

                    return null;

                }

                if (!RightPanelSignOutBtnRect.IsEmpty && RightPanelSignOutBtnRect.Contains(x, y))

                {

                    var confirm = MessageBox.Show(
                        AppL10n.T("Bạn có chắc chắn muốn đăng xuất khỏi tài khoản ZenQ này không?", "Are you sure you want to sign out of this ZenQ account?"),
                        AppL10n.T("Xác nhận đăng xuất", "Confirm Sign Out"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (confirm == DialogResult.Yes)
                    {
                        DatabaseAuthService.SignOut();
                        AppForm.Instance?.Invalidate();
                    }

                    return null;

                }



                // C. Toàn bộ vùng trung tâm thuộc quyền quản lý của TemplateDiscoveryView

                bool exitToHome;

                var tmplMode = _templateDiscoveryView.HandleClick(x, y, out openFilePath, out docTitle, out exitToHome);

                if (exitToHome)

                {

                    IsTemplateDiscoveryMode = false;

                    TargetScrollY = 0f;

                    SmoothScrollY = 0f;

                    return null;

                }

                if (tmplMode.HasValue)

                {

                    if (!string.IsNullOrEmpty(openFilePath) && !ZenQOffice.Core.TempCleanupManager.IsTempWorkFile(openFilePath))

                    {

                        AddRecentDocument(openFilePath, tmplMode.Value, docTitle);

                        SaveRecentDocs();

                    }

                    return tmplMode.Value;

                }



                // KHÓA CHẶT: Tuyệt đối không để sự kiện rơi xuống các thẻ tạo file hay danh sách tệp cũ của Trang chủ!

                return null;

            }



            // Search Box

            RectangleF searchBoxRect = GetSearchBoxBounds(width);

            if (ActiveTab != 2 && searchBoxRect.Contains(x, y))

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

            if (helpRect.Contains(x, y))

            {

                ShowHelpDialog();

                return null;

            }



            // User avatar / Update button area

            if (updateRect.Contains(x, y))

            {

                using (UpdateDialogForm dlg = new UpdateDialogForm(UpdateManager.LastReleaseInfo))

                {

                    dlg.ShowDialog();

                }

                return null;

            }



            // Creation cards (Chỉ có ở Trang chủ)

            if (ActiveTab == 0 && !IsTemplateDiscoveryMode && y >= 66)

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



            if (myDocsTab.Contains(x, y))

            {

                ActiveTab = 0;

                IsTemplateDiscoveryMode = false;

                SubFilter = 0;

                IndicatorTargetX = myDocsTab.X;

                IndicatorTargetW = myDocsTab.Width;

                ScrollY = 0;

                TargetScrollY = 0;

                return null;

            }

            RectangleF tmplNavTab = GetTemplateDiscoveryNavBounds(width, height);

            if (tmplNavTab.Contains(x, y))

            {

                ActiveTab = 0;

                IsTemplateDiscoveryMode = true;

                SubFilter = 0;

                IndicatorTargetX = tmplNavTab.X;

                IndicatorTargetW = tmplNavTab.Width;

                ScrollY = 0;

                TargetScrollY = 0;

                return null;

            }

            if (openDocNav.Contains(x, y))

            {

                ActiveTab = 3;

                IsTemplateDiscoveryMode = false;

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

                IsTemplateDiscoveryMode = false;

                IndicatorTargetX = trashTab.X;

                IndicatorTargetW = trashTab.Width;

                ScrollY = 0;

                return null;

            }

            if (bugReportTab.Contains(x, y))

            {

                using (var dlg = new BugReportDialogForm())

                {

                    dlg.ShowDialog();

                }

                return null;

            }



            if (settingsTab.Contains(x, y))

            {

                ActiveTab = 2;

                IsTemplateDiscoveryMode = false;

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

                // Promo Button -> Chuyển đổi Khám phá mẫu / Trang chủ (KHÔNG mở Writer!)

                if (GetPromoBtnBounds(width).Contains(x, y))

                {

                    if (ActiveTab == 0 && IsTemplateDiscoveryMode)

                    {

                        IsTemplateDiscoveryMode = false;

                    }

                    else

                    {

                        ActiveTab = 0;

                        IsTemplateDiscoveryMode = true;

                    }

                    TargetScrollY = 0f;

                    SmoothScrollY = 0f;

                    return null;

                }



                // Quản lý ổ đĩa -> Mở Explorer

                if (GetRightSharedBounds(width).Contains(x, y))

                {

                    ActiveTab = 4;

                    IsTemplateDiscoveryMode = false;

                    TargetScrollY = 0f;

                    SmoothScrollY = 0f;

                    _ = CloudShareService.RefreshAndAutoDownloadAsync();

                    return null;

                }

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



                // "Xem tất cả" bên cạnh "Mẫu nổi bật" -> Mở trang Khám phá mẫu

                // Nút "Cập nhật ngay" ở góc dưới bên phải (dưới Tự động lưu bản thảo an toàn)

                if (RightPanelUpdateBtnRect.Contains(x, y))

                {

                    using (UpdateDialogForm dlg = new UpdateDialogForm(UpdateManager.LastReleaseInfo))

                    {

                        dlg.ShowDialog();

                    }

                    return null;

                }

                if (!RightPanelAuthBtnRect.IsEmpty && RightPanelAuthBtnRect.Contains(x, y))

                {

                    using (AuthDialogForm dlg = new AuthDialogForm())

                    {

                        dlg.ShowDialog();

                    }

                    AppForm.Instance?.Invalidate();

                    return null;

                }

                if (!RightPanelSignOutBtnRect.IsEmpty && RightPanelSignOutBtnRect.Contains(x, y))

                {

                    var confirm = MessageBox.Show(
                        AppL10n.T("Bạn có chắc chắn muốn đăng xuất khỏi tài khoản ZenQ này không?", "Are you sure you want to sign out of this ZenQ account?"),
                        AppL10n.T("Xác nhận đăng xuất", "Confirm Sign Out"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (confirm == DialogResult.Yes)
                    {
                        DatabaseAuthService.SignOut();
                        AppForm.Instance?.Invalidate();
                    }

                    return null;

                }

            }



            // Tab-specific actions

            if (ActiveTab == 4)

            {

                return _sharedFilesView.HandleClick(x, y, out docTitle, out openFilePath);

            }

            if (ActiveTab == 3)

            {

                if (MyDocsOpenFileBtnRect.Contains(x, y))

                {

                    using (OpenFileDialog ofd = new OpenFileDialog())

                    {

                        ofd.Title = AppL10n.T("Mở tài liệu văn phòng - ZENQ OFFICE", "Open Office Document - ZENQ OFFICE");

                        ofd.Filter = AppL10n.T(
                            "Tất cả tài liệu & Thiết kế (*.docx;*.xlsx;*.pptx;*.pdf;*.svg;*.png;*.jpg;*.jpeg)|*.docx;*.xlsx;*.pptx;*.pdf;*.svg;*.png;*.jpg;*.jpeg|Đồ họa Vector SVG (*.svg)|*.svg|Hình ảnh (*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.psd)|*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.psd|Word (*.docx)|*.docx|Excel (*.xlsx)|*.xlsx|PowerPoint (*.pptx)|*.pptx|PDF (*.pdf)|*.pdf|Tất cả tệp (*.*)|*.*",
                            "All Documents & Designs (*.docx;*.xlsx;*.pptx;*.pdf;*.svg;*.png;*.jpg;*.jpeg)|*.docx;*.xlsx;*.pptx;*.pdf;*.svg;*.png;*.jpg;*.jpeg|SVG Vector Graphics (*.svg)|*.svg|Images (*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.psd)|*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.psd|Word (*.docx)|*.docx|Excel (*.xlsx)|*.xlsx|PowerPoint (*.pptx)|*.pptx|PDF (*.pdf)|*.pdf|All Files (*.*)|*.*");

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

                if (y >= 66)

                {

                float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;

                GetCenterContentMetrics(width, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);

                RectangleF viewAllRect = new RectangleF(startX + contentW - 120f, fileListY - SmoothScrollY, 120f, 26f);

                    if (viewAllRect.Contains(x, y))

                    {

                        ActiveTab = 3;

                        SubFilter = 0;

                        ScrollY = 0;

                        TargetScrollY = 0;

                        return null;

                    }

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

                        var dr = MessageBox.Show(
                            AppL10n.T("Bạn có chắc chắn muốn dọn sạch tất cả tệp trong Thùng rác không?", "Are you sure you want to permanently empty all files in Trash?"),
                            AppL10n.T("Dọn sạch thùng rác", "Empty Trash"),
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (dr == DialogResult.Yes)

                        {

                            EmptyTrash();

                        }

                        return null;

                    }

                }

            }



            // File list clicks

            if ((ActiveTab == 0 && !IsTemplateDiscoveryMode) || ActiveTab == 1 || ActiveTab == 3)

            {

                List<RecentDocItem> currentList = GetFilteredDocs(ActiveTab);

                float startX, contentW, bannerH, cardY, fileListY, colHeaderY, listTopY;

                GetCenterContentMetrics(width, out startX, out contentW, out bannerH, out cardY, out fileListY, out colHeaderY, out listTopY);

                float startY = listTopY - currentScroll;

                int rowHeight = 56;

                int rowTotalH = rowHeight + 2;



                int maxScroll = GetMaxScroll(height, width);

                float hitMinY = (ActiveTab == 0 && !IsTemplateDiscoveryMode) ? Math.Max(66f, startY) : listTopY;

                if (x >= startX && x <= startX + contentW - (maxScroll > 0 ? 14f : 0f) && y >= hitMinY && y <= height - 4f)

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



                                // Click on Trash can icon -> move to trash

                                if (x >= dotX - 16f && x <= dotX + 16f)

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

                // Settings Category Tabs
                for (int ci = 0; ci < SettingsCategoryTabRects.Length; ci++)
                {
                    if (SettingsCategoryTabRects[ci].Contains(x, y))
                    {
                        SettingsCategoryIndex = ci;
                        TargetScrollY = 0;
                        SmoothScrollY = 0;
                        return null;
                    }
                }

                // Settings clicks

                if (SettingAiCopilotToggleRect.Contains(x, y)) { AppSettings.Instance.AiCopilotEnabled = !AppSettings.Instance.AiCopilotEnabled; AppSettings.Instance.Save(); return null; }

                if (SettingAiProviderGoogleRect.Contains(x, y)) { AppSettings.Instance.SelectedAiProvider = 0; AppSettings.Instance.Save(); return null; }

                if (SettingAiProviderOpenAiRect.Contains(x, y)) { AppSettings.Instance.SelectedAiProvider = 1; AppSettings.Instance.Save(); return null; }

                if (SettingAiProviderClaudeRect.Contains(x, y)) { AppSettings.Instance.SelectedAiProvider = 2; AppSettings.Instance.Save(); return null; }

                if (SettingAiProviderOllamaRect.Contains(x, y)) { AppSettings.Instance.SelectedAiProvider = 3; AppSettings.Instance.Save(); return null; }

                if (SettingAiConfigBtnRect.Contains(x, y)) { _settingPulseKey = "btn_ai_cfg"; _settingPulseVal = 1f; using (AiConfigDialog dlg = new AiConfigDialog()) { dlg.ShowDialog(); } return null; }

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

                if (SettingCheckUpdateBtnRect.Contains(x, y)) { _settingPulseKey = "btn_about_upd"; _settingPulseVal = 1f; using (UpdateDialogForm dlg = new UpdateDialogForm(UpdateManager.LastReleaseInfo)) { dlg.ShowDialog(); } return null; }

                if (SettingHelpBtnRect.Contains(x, y)) { _settingPulseKey = "btn_about_help"; _settingPulseVal = 1f; ShowHelpDialog(); return null; }

                if (SettingShortcutWriterRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("ZenQ Writer", "-writer", "icon\\writer.ico", "Soạn thảo văn bản ZenQ Writer"); DesktopShortcutManager.DeleteShortcut("GenOffice Writer"); AppForm.Instance?.Invalidate(); return null; }

                if (SettingShortcutCalcRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("ZenQ Calc", "-calc", "icon\\calc.ico", "Bảng tính ZenQ Calc"); DesktopShortcutManager.DeleteShortcut("GenOffice Calc"); AppForm.Instance?.Invalidate(); return null; }

                if (SettingShortcutSlidesRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("ZenQ Slides", "-slides", "icon\\present.ico", "Trình chiếu ZenQ Slides"); DesktopShortcutManager.DeleteShortcut("GenOffice Slides"); AppForm.Instance?.Invalidate(); return null; }

                if (SettingShortcutPdfRect.Contains(x, y)) { DesktopShortcutManager.ToggleShortcut("ZenQ PDF", "-pdf", "icon\\pdf.ico", "Đọc và chỉnh sửa ZenQ PDF"); DesktopShortcutManager.DeleteShortcut("GenOffice PDF"); AppForm.Instance?.Invalidate(); return null; }

                if (SettingAssocWriterRect.Contains(x, y)) { FileAssociationManager.ToggleAppAssociation("-word"); return null; }

                if (SettingAssocCalcRect.Contains(x, y)) { FileAssociationManager.ToggleAppAssociation("-calc"); return null; }

                if (SettingAssocSlidesRect.Contains(x, y)) { FileAssociationManager.ToggleAppAssociation("-slides"); return null; }

                if (SettingAssocPdfRect.Contains(x, y)) { 

                    IntPtr hwnd = Form.ActiveForm != null ? Form.ActiveForm.Handle : IntPtr.Zero;

                    FileAssociationManager.ToggleAppAssociation("-pdf", hwnd); 

                    return null; 

                }

                if (SettingAssocSetAllBtnRect.Contains(x, y)) { 
                    _settingPulseKey = "btn_assoc_setall"; _settingPulseVal = 1f;

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

                if (SettingAssocUnsetAllBtnRect.Contains(x, y)) { _settingPulseKey = "btn_assoc_unsetall"; _settingPulseVal = 1f; FileAssociationManager.UnregisterAllFileAssociations(); return null; }

                if (SettingAssocWinSettingsRect.Contains(x, y)) { _settingPulseKey = "btn_assoc_win"; _settingPulseVal = 1f; FileAssociationManager.OpenWindowsDefaultAppsSettings("ZenQOffice"); return null; }

            }



            return null;

        }

        #endregion



        private void DrawVectorTemplateIcon(Graphics g, float x, float y, float size, Color c)

        {

            using (Pen pen = new Pen(c, 1.4f))

            {

                float cell = (size - 3f) / 2f;

                g.DrawRectangle(pen, x, y, cell, cell);

                g.DrawRectangle(pen, x + cell + 3f, y, cell, cell);

                g.DrawRectangle(pen, x, y + cell + 3f, cell, cell);

                g.DrawRectangle(pen, x + cell + 3f, y + cell + 3f, cell, cell);

            }

        }



            }

}


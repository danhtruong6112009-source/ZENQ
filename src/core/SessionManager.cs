using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ZenQOffice.UI;

namespace ZenQOffice.Core
{
    public class TabSessionItem
    {
        public string Title { get; set; }
        public OfficeAppMode Mode { get; set; }
        public string FilePath { get; set; }
        public string DraftFile { get; set; }
        public string TempSourceFile { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsModified { get; set; }
        public int ActiveSlideIndex { get; set; }
        public int ActiveSheetIndex { get; set; }
        public int ActivePageIndex { get; set; }

        public TabSessionItem()
        {
            Title = "";
            FilePath = "";
            DraftFile = "";
            Mode = OfficeAppMode.Home;
            IsActive = false;
            IsModified = false;
        }
    }

    public class SessionData
    {
        public List<TabSessionItem> Tabs { get; set; }
        public int ActiveTabIndex { get; set; }
        public string ActiveTabTitle { get; set; }
        public bool IsHomeActive { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
        public bool IsMaximized { get; set; }

        public SessionData()
        {
            Tabs = new List<TabSessionItem>();
            ActiveTabIndex = -1;
            ActiveTabTitle = "";
            IsHomeActive = false;
            WindowWidth = 1366;
            WindowHeight = 800;
            IsMaximized = true;
        }
    }

    /// <summary>
    /// Quản lý lưu và phục hồi phiên làm việc (Session) của GenOffice khi tắt và mở lại app.
    /// Đảm bảo tự động kích hoạt và mở lại chính xác Tab và giữ nguyên 100% văn bản đã gõ nếu thoát đột ngột.
    /// </summary>
    public static class SessionManager
    {
        public static bool IsRestoring { get; private set; }

        /// <summary>
        /// Optional isolated directory override used by automated tests so session persistence
        /// can be verified without modifying the user's real %APPDATA% session data.
        /// </summary>
        public static string SessionDirectoryOverride { get; set; }

        private static string GetSessionBaseDir()
        {
            if (!string.IsNullOrEmpty(SessionDirectoryOverride))
            {
                if (!Directory.Exists(SessionDirectoryOverride))
                {
                    try { Directory.CreateDirectory(SessionDirectoryOverride); } catch { }
                }
                return SessionDirectoryOverride;
            }
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "GenOffice");
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); } catch { }
            }
            return dir;
        }

        private static string GetSessionFilePath()
        {
            return Path.Combine(GetSessionBaseDir(), "last_session.xml");
        }

        public static string GetAutosaveDir()
        {
            string dir = Path.Combine(GetSessionBaseDir(), "autosave");
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); } catch { }
            }
            return dir;
        }

        private static bool IsGenericUntitledTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return true;
            string t = title.Trim();
            string[] prefixes = new string[]
            {
                "Văn bản Mới", "Văn bản ", "Tài liệu ", "New Document", "Document ",
                "Bảng tính Mới", "Bảng tính ", "New Sheet", "Spreadsheet ",
                "Trình chiếu Mới", "Trình chiếu ", "Trình bày ", "New Slides", "Presentation ",
                "Tài liệu PDF", "Open PDF", "Thiết kế Mới", "Thiết kế ", "Design "
            };
            for (int i = 0; i < prefixes.Length; i++)
            {
                if (t.StartsWith(prefixes[i], StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static string TryRecoverFilePathFromRecentDocs(string title, OfficeAppMode mode)
        {
            try
            {
                if (IsGenericUntitledTitle(title)) return "";
                string recentPath = Path.Combine(GetSessionBaseDir(), "recent_docs.xml");
                if (!File.Exists(recentPath)) return "";

                var doc = new System.Xml.XmlDocument();
                doc.Load(recentPath);
                var nodes = doc.SelectNodes("//RecentDocs/Doc");
                if (nodes == null) return "";

                string cleanTitle = title.Replace(" *", "").Trim();
                foreach (System.Xml.XmlNode n in nodes)
                {
                    if (n.Attributes == null) continue;
                    string fpath = n.Attributes["path"] != null ? n.Attributes["path"].Value : "";
                    if (string.IsNullOrEmpty(fpath) || !File.Exists(fpath)) continue;
                    int mVal = 0;
                    if (n.Attributes["mode"] != null) int.TryParse(n.Attributes["mode"].Value, out mVal);
                    if ((OfficeAppMode)mVal != mode) continue;

                    string rTitle = n.Attributes["title"] != null ? n.Attributes["title"].Value : "";
                    string fname = Path.GetFileName(fpath);
                    if (string.Equals(fname, cleanTitle, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(rTitle, cleanTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        return fpath;
                    }
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// Cập nhật nhanh (<1ms) chỉ metadata phiên làm việc (Tab đang mở, vị trí slide/trang) mà không nén lại các tệp nháp nặng.
        /// Giúp bảo toàn chính xác Tab đang active ngay khi người dùng chuyển tab hoặc thêm/đóng tab.
        /// </summary>
        public static void SaveSessionMetadata(AppForm appForm)
        {
            if (IsRestoring || appForm == null || appForm.Ribbon == null || appForm.Ribbon.StandaloneMode) return;

            try
            {
                int activeDocIdx = appForm.Ribbon.ActiveDocIndex;
                bool isHome = (appForm.Ribbon.CurrentMode == OfficeAppMode.Home) ||
                              (activeDocIdx >= 0 && activeDocIdx < appForm.Ribbon.DocTabs.Count && appForm.Ribbon.DocTabs[activeDocIdx].Mode == OfficeAppMode.Home);

                if (!isHome && activeDocIdx >= 0 && activeDocIdx < appForm.Ribbon.DocTabs.Count)
                {
                    DocTab curTab = appForm.Ribbon.DocTabs[activeDocIdx];
                    if (curTab.Mode == OfficeAppMode.Slides && appForm.SlidesView != null)
                        curTab.ActiveSlideIndex = appForm.SlidesView.ActiveSlideIndex;
                    else if (curTab.Mode == OfficeAppMode.Calc && appForm.CalcView != null)
                        curTab.CalcActiveSheetIndex = appForm.CalcView.ActiveSheetIndex;
                    else if (curTab.Mode == OfficeAppMode.Pdf && appForm.PdfView != null)
                        curTab.PdfActivePageIndex = appForm.PdfView.ActivePageIndex;
                }

                SessionData session = new SessionData
                {
                    IsHomeActive = isHome,
                    ActiveTabIndex = -1,
                    ActiveTabTitle = (activeDocIdx >= 0 && activeDocIdx < appForm.Ribbon.DocTabs.Count)
                        ? (appForm.Ribbon.DocTabs[activeDocIdx].Title ?? "")
                        : (isHome ? "Trang chính" : ""),
                    WindowWidth = appForm.Width,
                    WindowHeight = appForm.Height,
                    IsMaximized = (appForm.WindowState == FormWindowState.Maximized)
                };

                for (int i = 0; i < appForm.Ribbon.DocTabs.Count; i++)
                {
                    DocTab tab = appForm.Ribbon.DocTabs[i];
                    if (tab.Mode == OfficeAppMode.Home) continue;

                    if (!string.IsNullOrEmpty(tab.FilePath) && TempCleanupManager.IsTempWorkFile(tab.FilePath) && string.IsNullOrEmpty(tab.TempSourceFile))
                    {
                        tab.TempSourceFile = tab.FilePath;
                    }

                    bool isTabActive = (!isHome && i == activeDocIdx);
                    if (isTabActive)
                    {
                        session.ActiveTabIndex = session.Tabs.Count;
                    }

                    session.Tabs.Add(new TabSessionItem
                    {
                        Title = tab.Title ?? "",
                        Mode = tab.Mode,
                        FilePath = tab.FilePath ?? "",
                        DraftFile = tab.DraftFile ?? "",
                        TempSourceFile = tab.TempSourceFile ?? "",
                        IsActive = isTabActive,
                        IsModified = tab.IsModified,
                        ActiveSlideIndex = tab.ActiveSlideIndex,
                        ActiveSheetIndex = tab.CalcActiveSheetIndex,
                        ActivePageIndex = tab.PdfActivePageIndex
                    });
                }

                if (session.Tabs.Count == 0)
                {
                    session.IsHomeActive = true;
                    session.ActiveTabIndex = -1;
                    session.ActiveTabTitle = "Trang chính";
                }

                WriteSessionXml(session);
            }
            catch { }
        }

        private static void WriteSessionXml(SessionData session)
        {
            string filePath = GetSessionFilePath();
            using (StreamWriter sw = new StreamWriter(filePath, false, new UTF8Encoding(false)))
            {
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                sw.WriteLine("<GenOfficeSession>");
                sw.WriteLine(string.Format("  <Window width=\"{0}\" height=\"{1}\" maximized=\"{2}\" activeIndex=\"{3}\" activeTitle=\"{4}\" homeActive=\"{5}\"/>",
                    session.WindowWidth,
                    session.WindowHeight,
                    session.IsMaximized ? "1" : "0",
                    session.ActiveTabIndex,
                    EscapeXml(session.ActiveTabTitle),
                    session.IsHomeActive ? "1" : "0"));
                sw.WriteLine("  <Tabs>");
                foreach (var tab in session.Tabs)
                {
                    sw.WriteLine(string.Format("    <Tab title=\"{0}\" mode=\"{1}\" file=\"{2}\" draft=\"{3}\" active=\"{4}\" slide=\"{5}\" sheet=\"{6}\" page=\"{7}\" modified=\"{8}\" tempFile=\"{9}\"/>",
                        EscapeXml(tab.Title),
                        (int)tab.Mode,
                        EscapeXml(tab.FilePath),
                        EscapeXml(tab.DraftFile),
                        tab.IsActive ? "1" : "0",
                        tab.ActiveSlideIndex,
                        tab.ActiveSheetIndex,
                        tab.ActivePageIndex,
                        tab.IsModified ? "1" : "0",
                        EscapeXml(tab.TempSourceFile)));
                }
                sw.WriteLine("  </Tabs>");
                sw.WriteLine("</GenOfficeSession>");
            }
        }

        public static void SaveSession(AppForm appForm)
        {
            if (IsRestoring || appForm == null || appForm.Ribbon == null || appForm.Ribbon.StandaloneMode) return;

            try
            {
                // Đảm bảo trạng thái tài liệu hiện tại được lưu vào Tab
                appForm.Ribbon.SaveTabState(appForm.Ribbon.ActiveDocIndex, appForm.SlidesView, appForm.WriterView, appForm.CalcView, appForm.PdfView, appForm.PhotoView);

                int activeDocIdx = appForm.Ribbon.ActiveDocIndex;
                bool isHome = (appForm.Ribbon.CurrentMode == OfficeAppMode.Home) ||
                              (activeDocIdx >= 0 && activeDocIdx < appForm.Ribbon.DocTabs.Count && appForm.Ribbon.DocTabs[activeDocIdx].Mode == OfficeAppMode.Home);

                SessionData session = new SessionData();
                session.IsHomeActive = isHome;
                session.ActiveTabIndex = -1;
                if (activeDocIdx >= 0 && activeDocIdx < appForm.Ribbon.DocTabs.Count)
                {
                    session.ActiveTabTitle = isHome ? "Trang chính" : (appForm.Ribbon.DocTabs[activeDocIdx].Title ?? "");
                }

                session.WindowWidth = appForm.Width;
                session.WindowHeight = appForm.Height;
                session.IsMaximized = (appForm.WindowState == FormWindowState.Maximized);

                string autoSaveDir = GetAutosaveDir();

                for (int i = 0; i < appForm.Ribbon.DocTabs.Count; i++)
                {
                    DocTab tab = appForm.Ribbon.DocTabs[i];
                    if (tab.Mode == OfficeAppMode.Home) continue; // Home là tab mặc định

                    if (!string.IsNullOrEmpty(tab.FilePath) && ZenQOffice.Core.TempCleanupManager.IsTempWorkFile(tab.FilePath) && string.IsNullOrEmpty(tab.TempSourceFile))
                    {
                        tab.TempSourceFile = tab.FilePath;
                    }

                    string draftName = "";
                    string existingDraftPath = !string.IsNullOrEmpty(tab.DraftFile) ? Path.Combine(autoSaveDir, tab.DraftFile) : "";
                    bool hasExistingDraft = !string.IsNullOrEmpty(existingDraftPath) && File.Exists(existingDraftPath);
                    // Tối ưu tốc độ đóng app & bảo toàn 100% file gốc: nếu tệp đã tồn tại trên đĩa và KHÔNG có sửa đổi thì không ghi đè draft
                    bool hasValidDiskFile = !string.IsNullOrEmpty(tab.FilePath) && File.Exists(tab.FilePath);
                    bool needsDraft = tab.IsModified || !hasValidDiskFile;

                    // If this tab was deferred at startup and never clicked/materialized, keep its existing draft file intact
                    if (tab.NeedsLazyLoad && hasExistingDraft)
                    {
                        draftName = tab.DraftFile;
                    }
                    else if (needsDraft && tab.Mode == OfficeAppMode.Writer)
                    {
                        bool isCurActiveWriter = (i == appForm.Ribbon.ActiveDocIndex && appForm.WriterView != null);
                        string wText = (isCurActiveWriter && appForm.WriterView.PieceTable != null)
                            ? appForm.WriterView.PieceTable.GetText()
                            : ((tab.WriterPieceTable != null) ? tab.WriterPieceTable.GetText() : "");

                        var wShapes = isCurActiveWriter ? appForm.WriterView.DocumentShapes : tab.WriterShapes;
                        var wCharts = isCurActiveWriter ? appForm.WriterView.DocumentCharts : tab.WriterCharts;
                        var wTables = isCurActiveWriter ? appForm.WriterView.DocumentTables : tab.WriterDocumentTables;
                        string wHeader = isCurActiveWriter ? (appForm.WriterView.HeaderText ?? "") : (tab.WriterHeaderText ?? "");
                        string wFooter = isCurActiveWriter ? (appForm.WriterView.FooterText ?? "") : (tab.WriterFooterText ?? "");
                        bool hasRichContent = (wShapes != null && wShapes.Count > 0) ||
                                              (wCharts != null && wCharts.Count > 0) ||
                                              (wTables != null && wTables.Count > 0) ||
                                              !string.IsNullOrEmpty(wHeader) ||
                                              !string.IsNullOrEmpty(wFooter);

                        // Safety: never write binary data as plain-text draft, but still persist image/shape/table-only docs
                        bool safeText = !string.IsNullOrEmpty(wText) && wText.IndexOf('\x00') < 0;
                        bool safeToSave = safeText || (string.IsNullOrEmpty(wText) && hasRichContent);
                        if (safeToSave)
                        {
                            draftName = string.Format("draft_w_{0}.docx", i);
                            string draftPath = Path.Combine(autoSaveDir, draftName);
                            bool savedOk = false;
                            try
                            {
                                if (isCurActiveWriter)
                                {
                                    savedOk = appForm.WriterView.SaveDocumentFile(draftPath, appForm.Ribbon, true);
                                }
                                else
                                {
                                    savedOk = Format.DocxHandler.SaveDocxFile(
                                        draftPath,
                                        wText ?? "",
                                        tab.WriterFormattedRuns,
                                        tab.WriterPaperWidthInches > tab.WriterPaperHeightInches,
                                        tab.WriterMarginMode,
                                        tab.WriterHeaderText ?? "",
                                        tab.WriterFooterText ?? "",
                                        tab.WriterShowPageNumbers,
                                        tab.WriterShapes,
                                        tab.WriterCharts,
                                        tab.WriterParagraphAlignments,
                                        tab.WriterParagraphLineSpacings,
                                        tab.WriterParagraphSpacingsBefore,
                                        tab.WriterParagraphSpacingsAfter,
                                        tab.WriterParagraphBorders,
                                        tab.WriterDocumentTables,
                                        tab.WriterParagraphFirstLineIndents,
                                        tab.WriterParagraphLeftIndents,
                                        tab.WriterParagraphRightIndents,
                                        tab.WriterParagraphHangingIndents,
                                        tab.WriterCustomLeftMargin,
                                        tab.WriterCustomRightMargin,
                                        tab.WriterCustomTopMargin,
                                        tab.WriterCustomBottomMargin,
                                        tab.WriterPaperWidthInches,
                                        tab.WriterPaperHeightInches);
                                }
                                if (savedOk && File.Exists(draftPath))
                                {
                                    tab.DraftFile = draftName;
                                }
                                else
                                {
                                    draftName = "";
                                }
                            }
                            catch
                            {
                                savedOk = false;
                                draftName = "";
                            }

                            if (!savedOk && safeText)
                            {
                                string txtName = string.Format("draft_w_{0}.txt", i);
                                string fallbackPath = Path.Combine(autoSaveDir, txtName);
                                try
                                {
                                    File.WriteAllText(fallbackPath, wText, Encoding.UTF8);
                                    draftName = txtName;
                                    tab.DraftFile = txtName;
                                }
                                catch { draftName = ""; }
                            }
                        }

                        if (string.IsNullOrEmpty(draftName) && hasExistingDraft)
                        {
                            draftName = tab.DraftFile;
                        }
                    }
                    else if (needsDraft && tab.Mode == OfficeAppMode.Slides)
                    {
                        PresentationRoot pres = (i == appForm.Ribbon.ActiveDocIndex && appForm.SlidesView != null) 
                            ? appForm.SlidesView.Presentation 
                            : tab.PresentationData;

                        bool presIsBlank = pres == null || pres.Slides == null || pres.Slides.Count == 0 ||
                            (pres.Slides.Count == 1 && (pres.Slides[0].Shapes == null || pres.Slides[0].Shapes.Count == 0));

                        if (presIsBlank && !string.IsNullOrEmpty(tab.FilePath) && File.Exists(tab.FilePath))
                        {
                            try { pres = Format.PptxHandler.LoadPptx(tab.FilePath); tab.PresentationData = pres; } catch { }
                        }
                        if (presIsBlank && !string.IsNullOrEmpty(tab.TempSourceFile) && File.Exists(tab.TempSourceFile))
                        {
                            try { pres = Format.PptxHandler.LoadPptx(tab.TempSourceFile); tab.PresentationData = pres; } catch { }
                        }

                        if (pres != null && pres.Slides != null && pres.Slides.Count > 0)
                        {
                            draftName = string.Format("draft_s_{0}.pptx", i);
                            string draftPath = Path.Combine(autoSaveDir, draftName);
                            try { Format.PptxHandler.SavePptx(draftPath, pres); tab.DraftFile = draftName; } catch { }
                        }
                        else if (!string.IsNullOrEmpty(tab.DraftFile) && File.Exists(Path.Combine(autoSaveDir, tab.DraftFile)))
                        {
                            draftName = tab.DraftFile;
                        }
                    }
                    else if (needsDraft && tab.Mode == OfficeAppMode.Pdf)
                    {
                        PdfDocument pdoc = (i == appForm.Ribbon.ActiveDocIndex && appForm.PdfView != null && appForm.PdfView.Document != null)
                            ? appForm.PdfView.Document
                            : tab.PdfData;

                        if (pdoc == null && !string.IsNullOrEmpty(tab.FilePath) && File.Exists(tab.FilePath))
                        {
                            try { pdoc = Format.PdfHandler.LoadPdf(tab.FilePath); tab.PdfData = pdoc; } catch { }
                        }

                        if (pdoc != null && pdoc.Pages != null && pdoc.Pages.Count > 0)
                        {
                            draftName = string.Format("draft_p_{0}.pdf", i);
                            string draftPath = Path.Combine(autoSaveDir, draftName);
                            try
                            {
                                Format.PdfHandler.SavePdf(pdoc, draftPath);
                                tab.DraftFile = draftName;
                            }
                            catch { }
                        }
                        else if (!string.IsNullOrEmpty(tab.DraftFile) && File.Exists(Path.Combine(autoSaveDir, tab.DraftFile)))
                        {
                            draftName = tab.DraftFile;
                        }
                    }
                    else if (needsDraft && tab.Mode == OfficeAppMode.Photo)
                    {
                        PhotoDocument phdoc = (i == appForm.Ribbon.ActiveDocIndex && appForm.PhotoView != null && appForm.PhotoView.Document != null)
                            ? appForm.PhotoView.Document
                            : tab.PhotoData;

                        if (phdoc == null && !string.IsNullOrEmpty(tab.FilePath) && File.Exists(tab.FilePath))
                        {
                            try { phdoc = PhotoDocument.LoadFromFile(tab.FilePath); tab.PhotoData = phdoc; } catch { }
                        }

                        if (phdoc != null && phdoc.Width > 0 && phdoc.Height > 0)
                        {
                            draftName = string.Format("draft_ph_{0}.png", i);
                            string draftPath = Path.Combine(autoSaveDir, draftName);
                            try
                            {
                                phdoc.SaveToFile(draftPath);
                                tab.DraftFile = draftName;
                            }
                            catch { }
                        }
                        else if (!string.IsNullOrEmpty(tab.DraftFile) && File.Exists(Path.Combine(autoSaveDir, tab.DraftFile)))
                        {
                            draftName = tab.DraftFile;
                        }
                    }
                    else if (needsDraft && tab.Mode == OfficeAppMode.Calc)
                    {
                        CalcView cv = (i == appForm.Ribbon.ActiveDocIndex && appForm.CalcView != null) ? appForm.CalcView : null;
                        draftName = string.Format("draft_c_{0}.xlsx", i);
                        string draftPath = Path.Combine(autoSaveDir, draftName);
                        try
                        {
                            if (cv != null && cv.Sheets != null && cv.Sheets.Count > 0)
                            {
                                Format.XlsxHandler.SaveXlsx(draftPath, cv);
                                tab.DraftFile = draftName;
                            }
                            else if (tab.CalcSheets != null && tab.CalcSheets.Count > 0)
                            {
                                CalcView tempCv = new CalcView();
                                tempCv.Sheets = tab.CalcSheets;
                                tempCv.SheetNames = tab.CalcSheetNames;
                                if (tab.CalcColWidths != null) tempCv.CustomColWidths = new Dictionary<uint, int>(tab.CalcColWidths);
                                if (tab.CalcRowHeights != null) tempCv.CustomRowHeights = new Dictionary<uint, int>(tab.CalcRowHeights);
                                Format.XlsxHandler.SaveXlsx(draftPath, tempCv);
                                tab.DraftFile = draftName;
                            }
                        }
                        catch { }
                    }
                    else if (!needsDraft && !string.IsNullOrEmpty(tab.DraftFile) && File.Exists(Path.Combine(autoSaveDir, tab.DraftFile)))
                    {
                        draftName = tab.DraftFile;
                    }

                    bool isTabActive = (!isHome && i == activeDocIdx);
                    if (isTabActive)
                    {
                        session.ActiveTabIndex = session.Tabs.Count;
                    }

                    TabSessionItem item = new TabSessionItem
                    {
                        Title = tab.Title ?? "",
                        Mode = tab.Mode,
                        FilePath = tab.FilePath ?? "",
                        DraftFile = draftName,
                        TempSourceFile = tab.TempSourceFile ?? "",
                        IsActive = isTabActive,
                        IsModified = tab.IsModified,
                        ActiveSlideIndex = tab.ActiveSlideIndex,
                        ActiveSheetIndex = tab.CalcActiveSheetIndex,
                        ActivePageIndex = tab.PdfActivePageIndex
                    };
                    session.Tabs.Add(item);
                }

                if (session.Tabs.Count == 0)
                {
                    session.IsHomeActive = true;
                    session.ActiveTabIndex = -1;
                    session.ActiveTabTitle = "Trang chính";
                }

                // Loại bỏ logic chặn ghi session khi không có tab nào (cho phép lưu trạng thái trống khi đóng hết tab)

                WriteSessionXml(session);
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(GetAutosaveDir(), "crash.log"), ex.ToString());
            }
        }

        public static SessionData LoadSession()
        {
            SessionData session = new SessionData();
            string filePath = GetSessionFilePath();
            bool loadedFromXml = false;

            if (File.Exists(filePath))
            {
                try
                {
                    string xml = File.ReadAllText(filePath, Encoding.UTF8);
                    System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                    doc.LoadXml(xml);

                    var winNode = doc.SelectSingleNode("//Window");
                    if (winNode != null && winNode.Attributes != null)
                    {
                        int w, h, a;
                        if (int.TryParse(winNode.Attributes["width"] != null ? winNode.Attributes["width"].Value : "1366", out w)) session.WindowWidth = w;
                        if (int.TryParse(winNode.Attributes["height"] != null ? winNode.Attributes["height"].Value : "800", out h)) session.WindowHeight = h;
                        session.IsMaximized = (winNode.Attributes["maximized"] != null && winNode.Attributes["maximized"].Value == "1");
                        if (int.TryParse(winNode.Attributes["activeIndex"] != null ? winNode.Attributes["activeIndex"].Value : "-1", out a)) session.ActiveTabIndex = a;
                        if (winNode.Attributes["activeTitle"] != null) session.ActiveTabTitle = winNode.Attributes["activeTitle"].Value;
                        if (winNode.Attributes["homeActive"] != null) session.IsHomeActive = (winNode.Attributes["homeActive"].Value == "1");
                    }

                    var tabNodes = doc.SelectNodes("//Tabs/Tab");
                    if (tabNodes != null)
                    {
                        foreach (System.Xml.XmlNode n in tabNodes)
                        {
                            if (n.Attributes == null) continue;
                            string title = n.Attributes["title"] != null ? n.Attributes["title"].Value : "";
                            int modeVal = 0;
                            if (n.Attributes["mode"] != null) int.TryParse(n.Attributes["mode"].Value, out modeVal);
                            string file = n.Attributes["file"] != null ? n.Attributes["file"].Value : "";
                            string draft = n.Attributes["draft"] != null ? n.Attributes["draft"].Value : "";
                            string tempFile = n.Attributes["tempFile"] != null ? n.Attributes["tempFile"].Value : "";
                            bool active = (n.Attributes["active"] != null && n.Attributes["active"].Value == "1");
                            bool isMod = (n.Attributes["modified"] != null && n.Attributes["modified"].Value == "1");
                            int slide = 0, sheet = 0, page = 0;
                            if (n.Attributes["slide"] != null) int.TryParse(n.Attributes["slide"].Value, out slide);
                            if (n.Attributes["sheet"] != null) int.TryParse(n.Attributes["sheet"].Value, out sheet);
                            if (n.Attributes["page"] != null) int.TryParse(n.Attributes["page"].Value, out page);

                            session.Tabs.Add(new TabSessionItem
                            {
                                Title = title,
                                Mode = (OfficeAppMode)modeVal,
                                FilePath = file,
                                DraftFile = draft,
                                TempSourceFile = tempFile,
                                IsActive = active,
                                IsModified = isMod,
                                ActiveSlideIndex = slide,
                                ActiveSheetIndex = sheet,
                                ActivePageIndex = page
                            });
                        }
                    }
                    loadedFromXml = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERR LOAD SESSION: " + ex.Message);
                }
            }

            // Fallback: Nếu không thể đọc từ XML (hoặc chưa có file) và thư mục autosave có file nháp chưa lưu, tự động nạp
            if (!loadedFromXml && session.Tabs.Count == 0)
            {
                string autoSaveDir = GetAutosaveDir();
                if (Directory.Exists(autoSaveDir))
                {
                    var draftFiles = Directory.GetFiles(autoSaveDir);
                    foreach (var df in draftFiles)
                    {
                        string fname = Path.GetFileName(df);
                        if (fname.StartsWith("draft_c_") && fname.EndsWith(".xlsx"))
                        {
                            session.Tabs.Add(new TabSessionItem
                            {
                                Title = "Bảng tính 1",
                                Mode = OfficeAppMode.Calc,
                                DraftFile = fname,
                                IsActive = true
                            });
                        }
                        else if (fname.StartsWith("draft_s_") && fname.EndsWith(".pptx"))
                        {
                            string numStr = fname.Replace("draft_s_", "").Replace(".pptx", "");
                            session.Tabs.Add(new TabSessionItem
                            {
                                Title = "Trình bày " + numStr,
                                Mode = OfficeAppMode.Slides,
                                DraftFile = fname,
                                IsActive = (session.Tabs.Count == 0)
                            });
                        }
                        else if (fname.StartsWith("draft_w_") && (fname.EndsWith(".txt") || fname.EndsWith(".docx")))
                        {
                            session.Tabs.Add(new TabSessionItem
                            {
                                Title = "Tài liệu 1",
                                Mode = OfficeAppMode.Writer,
                                DraftFile = fname,
                                IsActive = false
                            });
                        }
                    }
                }
            }

            return session;
        }

        public static void RestoreSessionIntoApp(AppForm appForm, Action<string, float> progressCallback = null)
        {
            if (appForm == null) return;
            SessionData session = LoadSession();
            if (session == null || session.Tabs == null || session.Tabs.Count == 0)
            {
                if (progressCallback != null) progressCallback(AppL10n.T("Đang mở Trang chính...", "Opening Home..."), 1.0f);
                return;
            }

            IsRestoring = true;
            try
            {
                int total = session.Tabs.Count;
                int targetActiveIndex = 0;
                string autoSaveDir = GetAutosaveDir();

                // Determine which index is the active tab up-front:
                // 1) Explicit active="1" flag on <Tab> (authoritative, immune to duplicate titles or Home offset)
                int activeSessionTabIdx = -1;
                for (int i = 0; i < total; i++)
                {
                    if (session.Tabs[i].IsActive)
                    {
                        activeSessionTabIdx = i;
                        break;
                    }
                }

                bool titleMeansHome = string.Equals(session.ActiveTabTitle, "Trang chính", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(session.ActiveTabTitle, "Trang chủ", StringComparison.OrdinalIgnoreCase);

                // 2) Fallback when no <Tab> had active="1"
                if (activeSessionTabIdx < 0 && !session.IsHomeActive)
                {
                    if (!string.IsNullOrEmpty(session.ActiveTabTitle) && !titleMeansHome)
                    {
                        if (session.ActiveTabIndex >= 0 && session.ActiveTabIndex < total &&
                            session.Tabs[session.ActiveTabIndex].Title == session.ActiveTabTitle)
                        {
                            activeSessionTabIdx = session.ActiveTabIndex;
                        }
                        else
                        {
                            for (int i = 0; i < total; i++)
                            {
                                if (session.Tabs[i].Title == session.ActiveTabTitle)
                                { activeSessionTabIdx = i; break; }
                            }
                        }
                    }

                    if (activeSessionTabIdx < 0 && !titleMeansHome)
                    {
                        activeSessionTabIdx = (session.ActiveTabIndex >= 0 && session.ActiveTabIndex < total) ? session.ActiveTabIndex : 0;
                    }
                }

                int restoredCount = 0;
                for (int i = 0; i < total; i++)
                {
                    TabSessionItem item = session.Tabs[i];
                    if (item.Mode == OfficeAppMode.Home) continue;
                    if (string.IsNullOrEmpty(item.FilePath) && string.IsNullOrEmpty(item.DraftFile) && !item.IsModified)
                    {
                        string recoveredFile = TryRecoverFilePathFromRecentDocs(item.Title, item.Mode);
                        if (!string.IsNullOrEmpty(recoveredFile))
                            item.FilePath = recoveredFile;
                    }

                    // Skip empty untouched tabs with no file, no draft content and no modification
                    if (string.IsNullOrEmpty(item.FilePath) && string.IsNullOrEmpty(item.TempSourceFile) && string.IsNullOrEmpty(item.DraftFile) && !item.IsModified && string.IsNullOrEmpty(item.Title))
                    {
                        continue;
                    }

                    string displayTitle = !string.IsNullOrEmpty(item.Title) ? item.Title : "Tài liệu " + (restoredCount + 1);

                    appForm.Ribbon.AddDocTab(displayTitle, item.Mode, null, null, null, null, null, false);
                    int currentIdx = appForm.Ribbon.DocTabs.Count - 1;
                    restoredCount++;

                    if (!string.IsNullOrEmpty(item.FilePath))
                        appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath;
                    if (!string.IsNullOrEmpty(item.DraftFile))
                        appForm.Ribbon.DocTabs[currentIdx].DraftFile = item.DraftFile;
                    if (!string.IsNullOrEmpty(item.TempSourceFile))
                        appForm.Ribbon.DocTabs[currentIdx].TempSourceFile = item.TempSourceFile;
                    appForm.Ribbon.DocTabs[currentIdx].ActiveSlideIndex = item.ActiveSlideIndex;
                    appForm.Ribbon.DocTabs[currentIdx].CalcActiveSheetIndex = item.ActiveSheetIndex;
                    appForm.Ribbon.DocTabs[currentIdx].PdfActivePageIndex = item.ActivePageIndex;
                    appForm.Ribbon.DocTabs[currentIdx].IsModified = item.IsModified;

                    bool isActiveTab = (i == activeSessionTabIdx);

                    if (progressCallback != null)
                        progressCallback(string.Format(AppL10n.T("Đang nạp: {0}...", "Loading: {0}..."), displayTitle), 0.7f);

                    string draftPath = !string.IsNullOrEmpty(item.DraftFile) ? Path.Combine(autoSaveDir, item.DraftFile) : "";
                    bool hasDraft = !string.IsNullOrEmpty(draftPath) && File.Exists(draftPath);
                    bool hasFile = !string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath);
                    bool loaded = false;
                    bool deferLazyLoad = false;

                    if (!isActiveTab && item.Mode == OfficeAppMode.Writer && (hasDraft || hasFile))
                    {
                        deferLazyLoad = true;
                        loaded = true;
                    }

                    // 1. If tab was modified and draft exists, restore unsaved changes from draft
                    if (!loaded && item.IsModified && hasDraft)
                    {
                        try
                        {
                            if (item.Mode == OfficeAppMode.Writer && appForm.WriterView != null)
                            {
                                bool preferOriginal = false;
                                if (hasFile && File.Exists(item.FilePath))
                                {
                                    try
                                    {
                                        var fInfoOrig = new FileInfo(item.FilePath);
                                        var fInfoDraft = new FileInfo(draftPath);
                                        if (fInfoOrig.LastWriteTimeUtc >= fInfoDraft.LastWriteTimeUtc)
                                        {
                                            preferOriginal = true;
                                        }
                                    }
                                    catch { }
                                }

                                if (!preferOriginal)
                                {
                                    string ext = Path.GetExtension(draftPath).ToLower();
                                    if (ext == ".docx")
                                    {
                                        loaded = appForm.WriterView.LoadDocumentFile(draftPath, appForm.Ribbon);
                                        if (loaded && hasFile)
                                            appForm.WriterView.RestoreBorderlessTablesFromOriginalDocx(item.FilePath);
                                    }
                                    else if (IsSafeDraftText(draftPath))
                                    {
                                        string draftText = File.ReadAllText(draftPath, Encoding.UTF8);
                                        if (!string.IsNullOrEmpty(draftText))
                                        {
                                            appForm.WriterView.FormattedRuns.Clear();
                                            appForm.WriterView.DocumentShapes.Clear();
                                            appForm.WriterView.DocumentCharts.Clear();
                                            appForm.WriterView.DocumentTables.Clear();
                                            appForm.WriterView.PieceTable.SetInitialText(draftText);
                                            appForm.WriterView.InvalidateLayoutAndTextCache();
                                            appForm.WriterView.CursorPosition = draftText.Length;
                                            loaded = true;
                                        }
                                    }
                                    if (loaded)
                                    {
                                        appForm.WriterView.CurrentFilePath = item.FilePath ?? "";
                                        appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath ?? "";
                                        appForm.Ribbon.DocTabs[currentIdx].Title = displayTitle;
                                        appForm.Ribbon.DocTabs[currentIdx].IsModified = true;
                                    }
                                }
                            }
                            else if (item.Mode == OfficeAppMode.Slides)
                            {
                                var pres = Format.PptxHandler.LoadPptx(draftPath);
                                bool draftIsBlank = pres == null || pres.Slides == null || pres.Slides.Count == 0 ||
                                    (pres.Slides.Count == 1 && (pres.Slides[0].Shapes == null || pres.Slides[0].Shapes.Count == 0));

                                if (draftIsBlank && hasFile)
                                {
                                    try { pres = Format.PptxHandler.LoadPptx(item.FilePath); } catch { }
                                }
                                if ((pres == null || pres.Slides == null || pres.Slides.Count == 0) && !string.IsNullOrEmpty(item.TempSourceFile) && File.Exists(item.TempSourceFile))
                                {
                                    try { pres = Format.PptxHandler.LoadPptx(item.TempSourceFile); } catch { }
                                }

                                if (pres != null && pres.Slides != null && pres.Slides.Count > 0)
                                {
                                    appForm.Ribbon.DocTabs[currentIdx].PresentationData = pres;
                                    if (isActiveTab && appForm.SlidesView != null)
                                    {
                                        appForm.SlidesView.Presentation = pres;
                                        appForm.SlidesView.ActiveSlideIndex = Math.Max(0, Math.Min(pres.Slides.Count - 1, item.ActiveSlideIndex));
                                    }
                                    appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath ?? "";
                                    appForm.Ribbon.DocTabs[currentIdx].Title = displayTitle;
                                    appForm.Ribbon.DocTabs[currentIdx].IsModified = item.IsModified;
                                    loaded = true;
                                }
                            }
                            else if (item.Mode == OfficeAppMode.Pdf)
                            {
                                var pdoc = Format.PdfHandler.LoadPdf(draftPath);
                                if (pdoc != null && pdoc.Pages != null && pdoc.Pages.Count > 0)
                                {
                                    appForm.Ribbon.DocTabs[currentIdx].PdfData = pdoc;
                                    if (isActiveTab && appForm.PdfView != null)
                                    {
                                        appForm.PdfView.Document = pdoc;
                                        appForm.PdfView.ActivePageIndex = Math.Max(0, Math.Min(pdoc.Pages.Count - 1, item.ActivePageIndex));
                                    }
                                    appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath ?? "";
                                    appForm.Ribbon.DocTabs[currentIdx].Title = displayTitle;
                                    appForm.Ribbon.DocTabs[currentIdx].IsModified = item.IsModified;
                                    loaded = true;
                                }
                            }
                            else if (item.Mode == OfficeAppMode.Calc && !hasFile)
                            {
                                CalcView cv = (isActiveTab && appForm.CalcView != null) ? appForm.CalcView : new CalcView();
                                Format.XlsxHandler.LoadXlsxOrCsv(draftPath, cv);
                                appForm.Ribbon.DocTabs[currentIdx].CalcSheets = cv.Sheets;
                                appForm.Ribbon.DocTabs[currentIdx].CalcSheetNames = cv.SheetNames;
                                if (cv.CustomColWidths != null)
                                    appForm.Ribbon.DocTabs[currentIdx].CalcColWidths = new Dictionary<uint, int>(cv.CustomColWidths);
                                if (cv.CustomRowHeights != null)
                                    appForm.Ribbon.DocTabs[currentIdx].CalcRowHeights = new Dictionary<uint, int>(cv.CustomRowHeights);
                                if (isActiveTab && appForm.CalcView != null && appForm.CalcView.Sheets != null && appForm.CalcView.Sheets.Count > 0)
                                    appForm.CalcView.ActiveSheetIndex = Math.Max(0, Math.Min(appForm.CalcView.Sheets.Count - 1, item.ActiveSheetIndex));
                                appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath ?? "";
                                appForm.Ribbon.DocTabs[currentIdx].Title = displayTitle;
                                appForm.Ribbon.DocTabs[currentIdx].IsModified = true;
                                loaded = true;
                            }
                            else if (item.Mode == OfficeAppMode.Photo)
                            {
                                var phdoc = PhotoDocument.LoadFromFile(draftPath);
                                if (phdoc != null && phdoc.Width > 0 && phdoc.Height > 0)
                                {
                                    appForm.Ribbon.DocTabs[currentIdx].PhotoData = phdoc;
                                    if (isActiveTab && appForm.PhotoView != null)
                                        appForm.PhotoView.Document = phdoc;
                                    appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath ?? "";
                                    appForm.Ribbon.DocTabs[currentIdx].Title = displayTitle;
                                    appForm.Ribbon.DocTabs[currentIdx].IsModified = item.IsModified;
                                    loaded = true;
                                }
                            }
                        }
                        catch { }
                    }

                    // 2. If not loaded from draft, load original file from FilePath
                    if (!loaded && hasFile)
                    {
                        try
                        {
                            if (item.Mode == OfficeAppMode.Writer && appForm.WriterView != null)
                            {
                                // ActiveDocIndex is 0 (Home) during restore, so passing Ribbon updates paper/margin/font metrics without overwriting DocTabs[0]
                                loaded = appForm.WriterView.LoadDocumentFile(item.FilePath, appForm.Ribbon);
                                if (loaded)
                                {
                                    appForm.WriterView.CurrentFilePath = item.FilePath ?? "";
                                    appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath ?? "";
                                    appForm.Ribbon.DocTabs[currentIdx].Title = displayTitle;
                                    appForm.Ribbon.DocTabs[currentIdx].IsModified = item.IsModified;
                                }
                            }
                            else if (item.Mode == OfficeAppMode.Slides)
                            {
                                var pres = Format.PptxHandler.LoadPptx(item.FilePath);
                                if (pres != null && pres.Slides != null && pres.Slides.Count > 0)
                                {
                                    appForm.Ribbon.DocTabs[currentIdx].PresentationData = pres;
                                    if (isActiveTab && appForm.SlidesView != null)
                                    {
                                        appForm.SlidesView.Presentation = pres;
                                        appForm.SlidesView.ActiveSlideIndex = Math.Max(0, Math.Min(pres.Slides.Count - 1, item.ActiveSlideIndex));
                                    }
                                    loaded = true;
                                }
                            }
                            else if (item.Mode == OfficeAppMode.Calc)
                            {
                                CalcView cv = isActiveTab && appForm.CalcView != null ? appForm.CalcView : new CalcView();
                                Format.XlsxHandler.LoadXlsxOrCsv(item.FilePath, cv);
                                appForm.Ribbon.DocTabs[currentIdx].CalcSheets = cv.Sheets;
                                appForm.Ribbon.DocTabs[currentIdx].CalcSheetNames = cv.SheetNames;
                                if (cv.CustomColWidths != null)
                                    appForm.Ribbon.DocTabs[currentIdx].CalcColWidths = new Dictionary<uint, int>(cv.CustomColWidths);
                                if (cv.CustomRowHeights != null)
                                    appForm.Ribbon.DocTabs[currentIdx].CalcRowHeights = new Dictionary<uint, int>(cv.CustomRowHeights);
                                if (isActiveTab && appForm.CalcView != null && appForm.CalcView.Sheets != null && appForm.CalcView.Sheets.Count > 0)
                                    appForm.CalcView.ActiveSheetIndex = Math.Max(0, Math.Min(appForm.CalcView.Sheets.Count - 1, item.ActiveSheetIndex));
                                loaded = true;
                            }
                            else if (item.Mode == OfficeAppMode.Pdf)
                            {
                                var pdoc = Format.PdfHandler.LoadPdf(item.FilePath);
                                if (pdoc != null && pdoc.Pages != null && pdoc.Pages.Count > 0)
                                {
                                    appForm.Ribbon.DocTabs[currentIdx].PdfData = pdoc;
                                    if (isActiveTab && appForm.PdfView != null)
                                    {
                                        appForm.PdfView.Document = pdoc;
                                        appForm.PdfView.ActivePageIndex = Math.Max(0, Math.Min(pdoc.Pages.Count - 1, item.ActivePageIndex));
                                    }
                                    loaded = true;
                                }
                            }
                            else if (item.Mode == OfficeAppMode.Photo)
                            {
                                var phdoc = PhotoDocument.LoadFromFile(item.FilePath);
                                if (phdoc != null && phdoc.Width > 0 && phdoc.Height > 0)
                                {
                                    appForm.Ribbon.DocTabs[currentIdx].PhotoData = phdoc;
                                    if (isActiveTab && appForm.PhotoView != null)
                                        appForm.PhotoView.Document = phdoc;
                                    loaded = true;
                                }
                            }
                            if (loaded)
                            {
                                appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath;
                                appForm.Ribbon.DocTabs[currentIdx].Title = displayTitle;
                                appForm.Ribbon.DocTabs[currentIdx].IsModified = false;
                            }
                        }
                        catch { }
                    }

                    // 3. Fallback: if not loaded yet and has draft (e.g. unsaved doc without a FilePath)
                    if (!loaded && hasDraft)
                    {
                        try
                        {
                            if (item.Mode == OfficeAppMode.Writer && appForm.WriterView != null)
                            {
                                string ext = Path.GetExtension(draftPath).ToLower();
                                if (ext == ".docx")
                                {
                                    loaded = appForm.WriterView.LoadDocumentFile(draftPath, appForm.Ribbon);
                                    if (loaded)
                                        appForm.WriterView.CurrentFilePath = item.FilePath ?? "";
                                }
                                else if (IsSafeDraftText(draftPath))
                                {
                                    string draftText = File.ReadAllText(draftPath, Encoding.UTF8);
                                    if (!string.IsNullOrEmpty(draftText))
                                    {
                                        appForm.WriterView.FormattedRuns.Clear();
                                        appForm.WriterView.DocumentShapes.Clear();
                                        appForm.WriterView.DocumentCharts.Clear();
                                        appForm.WriterView.DocumentTables.Clear();
                                        appForm.WriterView.PieceTable.SetInitialText(draftText);
                                        appForm.WriterView.InvalidateLayoutAndTextCache();
                                        appForm.WriterView.CursorPosition = draftText.Length;
                                        loaded = true;
                                    }
                                }
                            }
                            else if (item.Mode == OfficeAppMode.Slides)
                            {
                                var pres = Format.PptxHandler.LoadPptx(draftPath);
                                if (pres != null && pres.Slides != null && pres.Slides.Count > 0)
                                {
                                    appForm.Ribbon.DocTabs[currentIdx].PresentationData = pres;
                                    if (isActiveTab && appForm.SlidesView != null)
                                    {
                                        appForm.SlidesView.Presentation = pres;
                                        appForm.SlidesView.ActiveSlideIndex = Math.Max(0, Math.Min(pres.Slides.Count - 1, item.ActiveSlideIndex));
                                    }
                                    loaded = true;
                                }
                            }
                            else if (item.Mode == OfficeAppMode.Calc)
                            {
                                CalcView cv = (isActiveTab && appForm.CalcView != null) ? appForm.CalcView : new CalcView();
                                Format.XlsxHandler.LoadXlsxOrCsv(draftPath, cv);
                                appForm.Ribbon.DocTabs[currentIdx].CalcSheets = cv.Sheets;
                                appForm.Ribbon.DocTabs[currentIdx].CalcSheetNames = cv.SheetNames;
                                if (cv.CustomColWidths != null)
                                    appForm.Ribbon.DocTabs[currentIdx].CalcColWidths = new Dictionary<uint, int>(cv.CustomColWidths);
                                if (cv.CustomRowHeights != null)
                                    appForm.Ribbon.DocTabs[currentIdx].CalcRowHeights = new Dictionary<uint, int>(cv.CustomRowHeights);
                                if (isActiveTab && appForm.CalcView != null && appForm.CalcView.Sheets != null && appForm.CalcView.Sheets.Count > 0)
                                    appForm.CalcView.ActiveSheetIndex = Math.Max(0, Math.Min(appForm.CalcView.Sheets.Count - 1, item.ActiveSheetIndex));
                                loaded = true;
                            }
                            else if (item.Mode == OfficeAppMode.Pdf)
                            {
                                var pdoc = Format.PdfHandler.LoadPdf(draftPath);
                                if (pdoc != null && pdoc.Pages != null && pdoc.Pages.Count > 0)
                                {
                                    appForm.Ribbon.DocTabs[currentIdx].PdfData = pdoc;
                                    if (isActiveTab && appForm.PdfView != null)
                                    {
                                        appForm.PdfView.Document = pdoc;
                                        appForm.PdfView.ActivePageIndex = Math.Max(0, Math.Min(pdoc.Pages.Count - 1, item.ActivePageIndex));
                                    }
                                    loaded = true;
                                }
                            }
                            else if (item.Mode == OfficeAppMode.Photo)
                            {
                                var phdoc = PhotoDocument.LoadFromFile(draftPath);
                                if (phdoc != null && phdoc.Width > 0 && phdoc.Height > 0)
                                {
                                    appForm.Ribbon.DocTabs[currentIdx].PhotoData = phdoc;
                                    if (isActiveTab && appForm.PhotoView != null)
                                        appForm.PhotoView.Document = phdoc;
                                    loaded = true;
                                }
                            }
                        }
                        catch { }
                    }

                    // 4. Fallback for Cloud Template from TempSourceFile or recover by title
                    if (item.Mode == OfficeAppMode.Slides)
                    {
                        bool isBlankSingleSlide = (appForm.Ribbon.DocTabs[currentIdx].PresentationData == null ||
                            appForm.Ribbon.DocTabs[currentIdx].PresentationData.Slides == null ||
                            appForm.Ribbon.DocTabs[currentIdx].PresentationData.Slides.Count <= 1);

                        if ((!loaded || isBlankSingleSlide) && !string.IsNullOrEmpty(item.TempSourceFile) && File.Exists(item.TempSourceFile))
                        {
                            try
                            {
                                var pres = Format.PptxHandler.LoadPptx(item.TempSourceFile);
                                if (pres != null && pres.Slides != null && pres.Slides.Count > 0)
                                {
                                    appForm.Ribbon.DocTabs[currentIdx].PresentationData = pres;
                                    if (isActiveTab && appForm.SlidesView != null)
                                    {
                                        appForm.SlidesView.Presentation = pres;
                                        appForm.SlidesView.ActiveSlideIndex = Math.Max(0, Math.Min(pres.Slides.Count - 1, item.ActiveSlideIndex));
                                    }
                                    loaded = true;
                                }
                            }
                            catch { }
                        }

                        if (!loaded || isBlankSingleSlide)
                        {
                            try
                            {
                                string recoveredPptx = ZenQOffice.Core.OnlineSlideTemplateManager.TryRecoverTemplateForTabByTitle(item.Title);
                                if (!string.IsNullOrEmpty(recoveredPptx) && File.Exists(recoveredPptx))
                                {
                                    appForm.Ribbon.DocTabs[currentIdx].TempSourceFile = recoveredPptx;
                                    var pres = Format.PptxHandler.LoadPptx(recoveredPptx);
                                    if (pres != null && pres.Slides != null && pres.Slides.Count > 0)
                                    {
                                        appForm.Ribbon.DocTabs[currentIdx].PresentationData = pres;
                                        if (isActiveTab && appForm.SlidesView != null)
                                        {
                                            appForm.SlidesView.Presentation = pres;
                                            appForm.SlidesView.ActiveSlideIndex = Math.Max(0, Math.Min(pres.Slides.Count - 1, item.ActiveSlideIndex));
                                        }
                                        loaded = true;
                                    }
                                }
                            }
                            catch { }
                        }
                    }

                    appForm.Ribbon.DocTabs[currentIdx].NeedsLazyLoad = deferLazyLoad;
                    if (isActiveTab)
                    {
                        appForm.Ribbon.SaveTabState(currentIdx, appForm.SlidesView, appForm.WriterView, appForm.CalcView, appForm.PdfView, appForm.PhotoView);
                        targetActiveIndex = currentIdx;
                    }
                }

                if (restoredCount == 0)
                {
                    appForm.Ribbon.CurrentMode = OfficeAppMode.Home;
                    appForm.Ribbon.ActiveDocIndex = 0;
                    if (appForm.Ribbon.DocTabs.Count > 0) appForm.Ribbon.DocTabs[0].Active = true;
                    appForm.Ribbon.EnsureActiveTabVisible();
                    appForm.Invalidate();
                    if (progressCallback != null) progressCallback(AppL10n.T("Đang mở Trang chính...", "Opening Home..."), 1.0f);
                    return;
                }

                // Switch to the active tab only if a real doc tab was restored and requested
                if (targetActiveIndex > 0 && targetActiveIndex < appForm.Ribbon.DocTabs.Count)
                {
                    try
                    {
                        appForm.Ribbon.SwitchDocTab(targetActiveIndex, appForm.SlidesView, appForm.WriterView, appForm.CalcView, appForm.PdfView, appForm.PhotoView);
                    }
                    catch
                    {
                        appForm.Ribbon.CurrentMode = OfficeAppMode.Home;
                        appForm.Ribbon.ActiveDocIndex = 0;
                        for (int t = 0; t < appForm.Ribbon.DocTabs.Count; t++)
                            appForm.Ribbon.DocTabs[t].Active = (t == 0);
                    }
                }
                else
                {
                    appForm.Ribbon.CurrentMode = OfficeAppMode.Home;
                    appForm.Ribbon.ActiveDocIndex = 0;
                    for (int t = 0; t < appForm.Ribbon.DocTabs.Count; t++)
                        appForm.Ribbon.DocTabs[t].Active = (t == 0);
                }

                appForm.Ribbon.EnsureActiveTabVisible();
                appForm.Invalidate();

                if (progressCallback != null)
                    progressCallback(AppL10n.T("Khôi phục phiên làm việc hoàn tất.", "Session restored."), 1.0f);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR RESTORE SESSION: " + ex.Message);
            }
            finally
            {
                IsRestoring = false;
                try { SaveSessionMetadata(appForm); } catch { }
            }
        }

        private static string EscapeXml(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        /// <summary>
        /// Validates that a draft .txt file is safe to load as plain text into WriterView.
        /// Rejects binary files (e.g. .doc saved with wrong extension) by checking for null bytes
        /// and ensuring the file size is reasonable.
        /// </summary>
        private static bool IsSafeDraftText(string path)
        {
            try
            {
                long fileSize = new FileInfo(path).Length;
                // Reject empty or excessively large files (> 2MB is suspicious for a draft text)
                if (fileSize == 0 || fileSize > 2 * 1024 * 1024) return false;

                // Read first 512 bytes to check for binary content (null bytes = binary)
                byte[] header = new byte[512];
                int bytesRead = 0;
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    bytesRead = fs.Read(header, 0, header.Length);
                }
                for (int i = 0; i < bytesRead; i++)
                {
                    if (header[i] == 0x00) return false; // Null byte = binary file
                }
                return true;
            }
            catch { return false; }
        }
    }
}



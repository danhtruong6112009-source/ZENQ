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
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
        public bool IsMaximized { get; set; }

        public SessionData()
        {
            Tabs = new List<TabSessionItem>();
            ActiveTabIndex = 0;
            ActiveTabTitle = "";
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
        private static string GetSessionFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "GenOffice");
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); } catch { }
            }
            return Path.Combine(dir, "last_session.xml");
        }

        private static string GetAutosaveDir()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "GenOffice", "autosave");
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); } catch { }
            }
            return dir;
        }

        public static void SaveSession(AppForm appForm)
        {
            if (appForm == null || appForm.Ribbon == null) return;

            try
            {
                // Đảm bảo trạng thái tài liệu hiện tại được lưu vào Tab
                appForm.Ribbon.SaveTabState(appForm.Ribbon.ActiveDocIndex, appForm.SlidesView, appForm.WriterView, appForm.CalcView, appForm.PdfView);

                SessionData session = new SessionData();
                session.ActiveTabIndex = appForm.Ribbon.ActiveDocIndex;
                if (appForm.Ribbon.ActiveDocIndex >= 0 && appForm.Ribbon.ActiveDocIndex < appForm.Ribbon.DocTabs.Count)
                {
                    session.ActiveTabTitle = appForm.Ribbon.DocTabs[appForm.Ribbon.ActiveDocIndex].Title ?? "";
                }

                session.WindowWidth = appForm.Width;
                session.WindowHeight = appForm.Height;
                session.IsMaximized = (appForm.WindowState == FormWindowState.Maximized);

                string autoSaveDir = GetAutosaveDir();

                for (int i = 0; i < appForm.Ribbon.DocTabs.Count; i++)
                {
                    DocTab tab = appForm.Ribbon.DocTabs[i];
                    if (tab.Mode == OfficeAppMode.Home) continue; // Home là tab mặc định

                    string draftName = "";
                    // Tối ưu tốc độ đóng app: nếu tệp đã lưu trên đĩa và KHÔNG có sửa đổi thì không cần nén draft nặng
                    bool needsDraft = tab.IsModified || string.IsNullOrEmpty(tab.FilePath);

                    if (needsDraft && tab.Mode == OfficeAppMode.Writer)
                    {
                        string wText = (i == appForm.Ribbon.ActiveDocIndex && appForm.WriterView != null && appForm.WriterView.PieceTable != null)
                            ? appForm.WriterView.PieceTable.GetText()
                            : ((tab.WriterPieceTable != null) ? tab.WriterPieceTable.GetText() : "");

                        // Safety: never write binary data as plain-text draft
                        bool safeToSave = !string.IsNullOrEmpty(wText) && wText.IndexOf('\x00') < 0;
                        if (safeToSave)
                        {
                            draftName = string.Format("draft_w_{0}.docx", i);
                            string draftPath = Path.Combine(autoSaveDir, draftName);
                            try
                            {
                                if (i == appForm.Ribbon.ActiveDocIndex && appForm.WriterView != null)
                                {
                                    appForm.WriterView.SaveDocumentFile(draftPath, appForm.Ribbon, true);
                                }
                                else
                                {
                                    Format.DocxHandler.SaveDocxFile(
                                        draftPath,
                                        wText,
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
                            }
                            catch
                            {
                                draftName = string.Format("draft_w_{0}.txt", i);
                                string fallbackPath = Path.Combine(autoSaveDir, draftName);
                                try { File.WriteAllText(fallbackPath, wText, Encoding.UTF8); } catch { }
                            }
                        }
                    }
                    else if (string.IsNullOrEmpty(tab.FilePath) && needsDraft && tab.Mode == OfficeAppMode.Slides)
                    {
                        PresentationRoot pres = (i == appForm.Ribbon.ActiveDocIndex && appForm.SlidesView != null) 
                            ? appForm.SlidesView.Presentation 
                            : tab.PresentationData;

                        if (pres != null && pres.Slides != null && pres.Slides.Count > 0)
                        {
                            draftName = string.Format("draft_s_{0}.pptx", i);
                            string draftPath = Path.Combine(autoSaveDir, draftName);
                            try { Format.PptxHandler.SavePptx(draftPath, pres); } catch { }
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
                            }
                            else if (tab.CalcSheets != null && tab.CalcSheets.Count > 0)
                            {
                                CalcView tempCv = new CalcView();
                                tempCv.Sheets = tab.CalcSheets;
                                tempCv.SheetNames = tab.CalcSheetNames;
                                if (tab.CalcColWidths != null) tempCv.CustomColWidths = new Dictionary<uint, int>(tab.CalcColWidths);
                                if (tab.CalcRowHeights != null) tempCv.CustomRowHeights = new Dictionary<uint, int>(tab.CalcRowHeights);
                                Format.XlsxHandler.SaveXlsx(draftPath, tempCv);
                            }
                        }
                        catch { }
                    }

                    // Always save tabs to preserve session state
                    // if (string.IsNullOrEmpty(tab.FilePath) && string.IsNullOrEmpty(draftName) && !tab.IsModified)
                    // {
                    //    continue;
                    // }

                    TabSessionItem item = new TabSessionItem
                    {
                        Title = tab.Title ?? "",
                        Mode = tab.Mode,
                        FilePath = tab.FilePath ?? "",
                        DraftFile = draftName,
                        IsActive = (i == appForm.Ribbon.ActiveDocIndex),
                        IsModified = tab.IsModified,
                        ActiveSlideIndex = tab.ActiveSlideIndex,
                        ActiveSheetIndex = tab.CalcActiveSheetIndex,
                        ActivePageIndex = tab.PdfActivePageIndex
                    };
                    session.Tabs.Add(item);
                }

                if (session.Tabs.Count == 0)
                {
                    session.ActiveTabIndex = 0;
                    session.ActiveTabTitle = "Trang chủ";
                }

                // Loại bỏ logic chặn ghi session khi không có tab nào (cho phép lưu trạng thái trống khi đóng hết tab)

                string filePath = GetSessionFilePath();
                using (StreamWriter sw = new StreamWriter(filePath, false, new UTF8Encoding(false)))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    sw.WriteLine("<GenOfficeSession>");
                    sw.WriteLine(string.Format("  <Window width=\"{0}\" height=\"{1}\" maximized=\"{2}\" activeIndex=\"{3}\" activeTitle=\"{4}\"/>",
                        session.WindowWidth, session.WindowHeight, session.IsMaximized ? "1" : "0", session.ActiveTabIndex, EscapeXml(session.ActiveTabTitle)));
                    sw.WriteLine("  <Tabs>");
                    foreach (var tab in session.Tabs)
                    {
                        sw.WriteLine(string.Format("    <Tab title=\"{0}\" mode=\"{1}\" file=\"{2}\" draft=\"{3}\" active=\"{4}\" slide=\"{5}\" sheet=\"{6}\" page=\"{7}\" modified=\"{8}\"/>",
                            EscapeXml(tab.Title),
                            (int)tab.Mode,
                            EscapeXml(tab.FilePath),
                            EscapeXml(tab.DraftFile),
                            tab.IsActive ? "1" : "0",
                            tab.ActiveSlideIndex,
                            tab.ActiveSheetIndex,
                            tab.ActivePageIndex,
                            tab.IsModified ? "1" : "0"));
                    }
                    sw.WriteLine("  </Tabs>");
                    sw.WriteLine("</GenOfficeSession>");
                }
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
                        if (int.TryParse(winNode.Attributes["activeIndex"] != null ? winNode.Attributes["activeIndex"].Value : "0", out a)) session.ActiveTabIndex = a;
                        if (winNode.Attributes["activeTitle"] != null) session.ActiveTabTitle = winNode.Attributes["activeTitle"].Value;
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
                if (progressCallback != null) progressCallback("Đang mở Trang chính...", 1.0f);
                return;
            }

            try
            {
                int total = session.Tabs.Count;
                int targetActiveIndex = 0;
                string autoSaveDir = GetAutosaveDir();

                // Determine which index is the active tab up-front
                int activeSessionTabIdx = -1;
                if (!string.IsNullOrEmpty(session.ActiveTabTitle))
                {
                    for (int i = 0; i < total; i++)
                    {
                        if (session.Tabs[i].Title == session.ActiveTabTitle)
                        { activeSessionTabIdx = i; break; }
                    }
                }
                if (activeSessionTabIdx < 0)
                    activeSessionTabIdx = (session.ActiveTabIndex >= 0 && session.ActiveTabIndex < total) ? session.ActiveTabIndex : 0;

                int restoredCount = 0;
                for (int i = 0; i < total; i++)
                {
                    TabSessionItem item = session.Tabs[i];
                    // Skip empty untouched tabs with no file, no draft content and no modification
                    if (string.IsNullOrEmpty(item.FilePath) && string.IsNullOrEmpty(item.DraftFile) && !item.IsModified)
                    {
                        continue;
                    }

                    string displayTitle = !string.IsNullOrEmpty(item.Title) ? item.Title : "Tài liệu " + (restoredCount + 1);

                    appForm.Ribbon.AddDocTab(displayTitle, item.Mode, appForm.SlidesView, appForm.WriterView, appForm.CalcView, appForm.PdfView);
                    int currentIdx = appForm.Ribbon.DocTabs.Count - 1;
                    restoredCount++;

                    if (!string.IsNullOrEmpty(item.FilePath))
                        appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath;
                    if (!string.IsNullOrEmpty(item.DraftFile))
                        appForm.Ribbon.DocTabs[currentIdx].DraftFile = item.DraftFile;
                    appForm.Ribbon.DocTabs[currentIdx].ActiveSlideIndex = item.ActiveSlideIndex;
                    appForm.Ribbon.DocTabs[currentIdx].CalcActiveSheetIndex = item.ActiveSheetIndex;
                    appForm.Ribbon.DocTabs[currentIdx].PdfActivePageIndex = item.ActivePageIndex;
                    appForm.Ribbon.DocTabs[currentIdx].IsModified = item.IsModified;

                    bool isActiveTab = (i == activeSessionTabIdx);

                    if (isActiveTab)
                    {
                        // Only fully load the ACTIVE tab — load others lazily when user clicks
                        if (progressCallback != null)
                            progressCallback(string.Format("Đang khôi phục: {0}...", displayTitle), 0.7f);

                        string draftPath = !string.IsNullOrEmpty(item.DraftFile) ? Path.Combine(autoSaveDir, item.DraftFile) : "";
                        bool hasDraft = !string.IsNullOrEmpty(draftPath) && File.Exists(draftPath);
                        bool hasFile = !string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath);
                        bool loaded = false;

                        // 1. If tab was modified and draft exists, restore unsaved changes from draft
                        if (item.IsModified && hasDraft)
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
                                        }
                                        else if (IsSafeDraftText(draftPath))
                                        {
                                            string draftText = File.ReadAllText(draftPath, Encoding.UTF8);
                                            if (!string.IsNullOrEmpty(draftText))
                                            {
                                                appForm.WriterView.PieceTable.SetInitialText(draftText);
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
                                else if (item.Mode == OfficeAppMode.Slides && appForm.SlidesView != null && !hasFile)
                                {
                                    appForm.SlidesView.Presentation = Format.PptxHandler.LoadPptx(draftPath);
                                    appForm.SlidesView.ActiveSlideIndex = Math.Max(0, Math.Min(appForm.SlidesView.Presentation.Slides.Count - 1, item.ActiveSlideIndex));
                                    appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath ?? "";
                                    appForm.Ribbon.DocTabs[currentIdx].Title = displayTitle;
                                    appForm.Ribbon.DocTabs[currentIdx].IsModified = true;
                                    loaded = true;
                                }
                                else if (item.Mode == OfficeAppMode.Calc && appForm.CalcView != null && !hasFile)
                                {
                                    Format.XlsxHandler.LoadXlsxOrCsv(draftPath, appForm.CalcView);
                                    appForm.CalcView.ActiveSheetIndex = Math.Max(0, Math.Min(appForm.CalcView.Sheets.Count - 1, item.ActiveSheetIndex));
                                    appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath ?? "";
                                    appForm.Ribbon.DocTabs[currentIdx].Title = displayTitle;
                                    appForm.Ribbon.DocTabs[currentIdx].IsModified = true;
                                    loaded = true;
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
                                    loaded = appForm.WriterView.LoadDocumentFile(item.FilePath, appForm.Ribbon);
                                }
                                else if (item.Mode == OfficeAppMode.Slides && appForm.SlidesView != null)
                                {
                                    appForm.SlidesView.Presentation = Format.PptxHandler.LoadPptx(item.FilePath);
                                    appForm.SlidesView.ActiveSlideIndex = Math.Max(0, Math.Min(appForm.SlidesView.Presentation.Slides.Count - 1, item.ActiveSlideIndex));
                                    loaded = true;
                                }
                                else if (item.Mode == OfficeAppMode.Calc && appForm.CalcView != null)
                                {
                                    Format.XlsxHandler.LoadXlsxOrCsv(item.FilePath, appForm.CalcView);
                                    appForm.CalcView.ActiveSheetIndex = Math.Max(0, Math.Min(appForm.CalcView.Sheets.Count - 1, item.ActiveSheetIndex));
                                    loaded = true;
                                }
                                else if (item.Mode == OfficeAppMode.Pdf && appForm.PdfView != null)
                                {
                                    appForm.PdfView.Document = Format.PdfHandler.LoadPdf(item.FilePath);
                                    appForm.PdfView.ActivePageIndex = Math.Max(0, Math.Min(appForm.PdfView.Document.Pages.Count - 1, item.ActivePageIndex));
                                    loaded = true;
                                }
                                if (loaded)
                                {
                                    appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath ?? "";
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
                                        appForm.WriterView.CurrentFilePath = item.FilePath ?? "";
                                        appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath ?? "";
                                        appForm.Ribbon.DocTabs[currentIdx].Title = displayTitle;
                                    }
                                    else if (IsSafeDraftText(draftPath))
                                    {
                                        string draftText = File.ReadAllText(draftPath, Encoding.UTF8);
                                        if (!string.IsNullOrEmpty(draftText))
                                        {
                                            appForm.WriterView.PieceTable.SetInitialText(draftText);
                                            appForm.WriterView.CursorPosition = draftText.Length;
                                            appForm.WriterView.CurrentFilePath = item.FilePath ?? "";
                                            appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath ?? "";
                                            appForm.Ribbon.DocTabs[currentIdx].Title = displayTitle;
                                            loaded = true;
                                        }
                                    }
                                }
                                else if (item.Mode == OfficeAppMode.Slides && appForm.SlidesView != null)
                                {
                                    appForm.SlidesView.Presentation = Format.PptxHandler.LoadPptx(draftPath);
                                    appForm.SlidesView.ActiveSlideIndex = Math.Max(0, Math.Min(appForm.SlidesView.Presentation.Slides.Count - 1, item.ActiveSlideIndex));
                                    loaded = true;
                                }
                                else if (item.Mode == OfficeAppMode.Calc && appForm.CalcView != null)
                                {
                                    Format.XlsxHandler.LoadXlsxOrCsv(draftPath, appForm.CalcView);
                                    appForm.CalcView.ActiveSheetIndex = Math.Max(0, Math.Min(appForm.CalcView.Sheets.Count - 1, item.ActiveSheetIndex));
                                    loaded = true;
                                }
                            }
                            catch { }
                        }

                        appForm.Ribbon.SaveTabState(currentIdx, appForm.SlidesView, appForm.WriterView, appForm.CalcView, appForm.PdfView);
                        targetActiveIndex = currentIdx;
                    }
                    else
                    {
                        // Inactive tabs: mark for lazy-load when user clicks on them
                        appForm.Ribbon.DocTabs[currentIdx].NeedsLazyLoad = true;
                    }
                }

                if (restoredCount == 0)
                {
                    appForm.Ribbon.CurrentMode = OfficeAppMode.Home;
                    appForm.Ribbon.ActiveDocIndex = 0;
                    if (appForm.Ribbon.DocTabs.Count > 0) appForm.Ribbon.DocTabs[0].Active = true;
                    appForm.Ribbon.EnsureActiveTabVisible();
                    appForm.Invalidate();
                    if (progressCallback != null) progressCallback("Đang mở Trang chính...", 1.0f);
                    return;
                }

                // Switch to the active tab only if a real doc tab was restored and requested
                if (targetActiveIndex > 0 && targetActiveIndex < appForm.Ribbon.DocTabs.Count)
                {
                    try
                    {
                        appForm.Ribbon.SwitchDocTab(targetActiveIndex, appForm.SlidesView, appForm.WriterView, appForm.CalcView, appForm.PdfView);
                    }
                    catch
                    {
                        // Fallback an toàn về Trang chủ Home nếu tab được khôi phục gặp lỗi
                        appForm.Ribbon.CurrentMode = OfficeAppMode.Home;
                        appForm.Ribbon.ActiveDocIndex = 0;
                        if (appForm.Ribbon.DocTabs.Count > 0) appForm.Ribbon.DocTabs[0].Active = true;
                    }
                }
                else
                {
                    appForm.Ribbon.CurrentMode = OfficeAppMode.Home;
                    appForm.Ribbon.ActiveDocIndex = 0;
                    if (appForm.Ribbon.DocTabs.Count > 0) appForm.Ribbon.DocTabs[0].Active = true;
                }

                appForm.Ribbon.EnsureActiveTabVisible();
                appForm.Invalidate();

                if (progressCallback != null)
                    progressCallback("Khôi phục phiên làm việc hoàn tất.", 1.0f);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR RESTORE SESSION: " + ex.Message);
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



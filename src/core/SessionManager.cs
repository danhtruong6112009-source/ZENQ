using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using GenOffice.UI;

namespace GenOffice.Core
{
    public class TabSessionItem
    {
        public string Title { get; set; }
        public OfficeAppMode Mode { get; set; }
        public string FilePath { get; set; }
        public bool IsActive { get; set; }
        public int ActiveSlideIndex { get; set; }
        public int ActiveSheetIndex { get; set; }
        public int ActivePageIndex { get; set; }

        public TabSessionItem()
        {
            Title = "";
            FilePath = "";
            Mode = OfficeAppMode.Home;
            IsActive = false;
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
            IsMaximized = false;
        }
    }

    /// <summary>
    /// Quản lý lưu và phục hồi phiên làm việc (Session) của GenOffice khi tắt và mở lại app.
    /// Đảm bảo tự động kích hoạt và mở lại chính xác Tab mà người dùng đang làm việc ở lần cuối cùng trước khi đóng app.
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

                for (int i = 0; i < appForm.Ribbon.DocTabs.Count; i++)
                {
                    DocTab tab = appForm.Ribbon.DocTabs[i];
                    if (tab.Mode == OfficeAppMode.Home) continue; // Home là tab mặc định

                    TabSessionItem item = new TabSessionItem
                    {
                        Title = tab.Title ?? "",
                        Mode = tab.Mode,
                        FilePath = tab.FilePath ?? "",
                        IsActive = (i == appForm.Ribbon.ActiveDocIndex),
                        ActiveSlideIndex = tab.ActiveSlideIndex,
                        ActiveSheetIndex = tab.CalcActiveSheetIndex,
                        ActivePageIndex = tab.PdfActivePageIndex
                    };
                    session.Tabs.Add(item);
                }

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
                        sw.WriteLine(string.Format("    <Tab title=\"{0}\" mode=\"{1}\" file=\"{2}\" active=\"{3}\" slide=\"{4}\" sheet=\"{5}\" page=\"{6}\"/>",
                            EscapeXml(tab.Title),
                            (int)tab.Mode,
                            EscapeXml(tab.FilePath),
                            tab.IsActive ? "1" : "0",
                            tab.ActiveSlideIndex,
                            tab.ActiveSheetIndex,
                            tab.ActivePageIndex));
                    }
                    sw.WriteLine("  </Tabs>");
                    sw.WriteLine("</GenOfficeSession>");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR SAVE SESSION: " + ex.Message);
            }
        }

        public static SessionData LoadSession()
        {
            string filePath = GetSessionFilePath();
            if (!File.Exists(filePath)) return null;

            try
            {
                SessionData session = new SessionData();
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
                        bool active = (n.Attributes["active"] != null && n.Attributes["active"].Value == "1");
                        int slide = 0, sheet = 0, page = 0;
                        if (n.Attributes["slide"] != null) int.TryParse(n.Attributes["slide"].Value, out slide);
                        if (n.Attributes["sheet"] != null) int.TryParse(n.Attributes["sheet"].Value, out sheet);
                        if (n.Attributes["page"] != null) int.TryParse(n.Attributes["page"].Value, out page);

                        session.Tabs.Add(new TabSessionItem
                        {
                            Title = title,
                            Mode = (OfficeAppMode)modeVal,
                            FilePath = file,
                            IsActive = active,
                            ActiveSlideIndex = slide,
                            ActiveSheetIndex = sheet,
                            ActivePageIndex = page
                        });
                    }
                }

                return session;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR LOAD SESSION: " + ex.Message);
                return null;
            }
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
                int targetActiveIndex = 0; // Mặc định là Trang chính nếu không có tab nào active

                for (int i = 0; i < total; i++)
                {
                    TabSessionItem item = session.Tabs[i];
                    float prog = 0.3f + ((float)(i + 1) / total) * 0.6f;

                    string displayTitle = !string.IsNullOrEmpty(item.Title) ? item.Title : "Tài liệu " + (i + 1);
                    if (progressCallback != null)
                    {
                        progressCallback(string.Format("Đang khôi phục: {0}...", displayTitle), prog);
                    }

                    // Thêm tab mới
                    appForm.Ribbon.AddDocTab(displayTitle, item.Mode, appForm.SlidesView, appForm.WriterView, appForm.CalcView, appForm.PdfView);
                    int currentIdx = appForm.Ribbon.DocTabs.Count - 1;

                    if (!string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                    {
                        appForm.Ribbon.DocTabs[currentIdx].FilePath = item.FilePath;

                        if (item.Mode == OfficeAppMode.Writer && appForm.WriterView != null)
                        {
                            appForm.WriterView.LoadDocumentFile(item.FilePath, appForm.Ribbon);
                        }
                        else if (item.Mode == OfficeAppMode.Slides && appForm.SlidesView != null)
                        {
                            appForm.SlidesView.Presentation = Format.PptxHandler.LoadPptx(item.FilePath);
                            appForm.SlidesView.ActiveSlideIndex = Math.Max(0, Math.Min(appForm.SlidesView.Presentation.Slides.Count - 1, item.ActiveSlideIndex));
                        }
                        else if (item.Mode == OfficeAppMode.Calc && appForm.CalcView != null)
                        {
                            Format.XlsxHandler.LoadXlsxOrCsv(item.FilePath, appForm.CalcView);
                            appForm.CalcView.ActiveSheetIndex = Math.Max(0, Math.Min(appForm.CalcView.Sheets.Count - 1, item.ActiveSheetIndex));
                        }
                        else if (item.Mode == OfficeAppMode.Pdf && appForm.PdfView != null)
                        {
                            appForm.PdfView.Document = Format.PdfHandler.LoadPdf(item.FilePath);
                            appForm.PdfView.ActivePageIndex = Math.Max(0, Math.Min(appForm.PdfView.Document.Pages.Count - 1, item.ActivePageIndex));
                        }
                    }

                    appForm.Ribbon.DocTabs[currentIdx].IsModified = false;
                    appForm.Ribbon.SaveTabState(currentIdx, appForm.SlidesView, appForm.WriterView, appForm.CalcView, appForm.PdfView);

                    if (item.IsActive)
                    {
                        targetActiveIndex = currentIdx;
                    }
                }

                // Nếu lưu theo tiêu đề Tab active trước đó
                if (!string.IsNullOrEmpty(session.ActiveTabTitle))
                {
                    for (int k = 0; k < appForm.Ribbon.DocTabs.Count; k++)
                    {
                        if (appForm.Ribbon.DocTabs[k].Title == session.ActiveTabTitle)
                        {
                            targetActiveIndex = k;
                            break;
                        }
                    }
                }
                else if (session.ActiveTabIndex >= 0 && session.ActiveTabIndex < appForm.Ribbon.DocTabs.Count)
                {
                    targetActiveIndex = session.ActiveTabIndex;
                }

                // Chuyển sang chính xác Tab đang hoạt động ở lần cuối đóng app
                if (targetActiveIndex >= 0 && targetActiveIndex < appForm.Ribbon.DocTabs.Count)
                {
                    appForm.Ribbon.SwitchDocTab(targetActiveIndex, appForm.SlidesView, appForm.WriterView, appForm.CalcView, appForm.PdfView);
                }

                appForm.Ribbon.EnsureActiveTabVisible();
                appForm.Invalidate();

                if (progressCallback != null)
                {
                    progressCallback("Khôi phục phiên làm việc hoàn tất.", 1.0f);
                }
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
    }
}

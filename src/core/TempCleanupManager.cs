using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using ZenQOffice.UI;

namespace ZenQOffice.Core
{
    /// <summary>
    /// Trình quản lý giải phóng bộ nhớ và dọn dẹp tệp tạm theo nguyên tắc Cloud-Only:
    /// - Mẫu (Templates), Đồ họa Vector (Graphics) và Hình ảnh (Stock Images) là 100% Cloud, tuyệt đối không lưu thư mục Templates hay Cache trong AppData.
    /// - Chỉ khi người dùng bấm chọn sử dụng mẫu hoặc chèn vector/ảnh vào Tab đang làm việc thì tệp đó mới được tải tạm vào thư mục làm việc của Tab.
    /// - Khi người dùng đóng Tab của tệp đó: Xóa NGAY LẬP TỨC tệp mẫu tạm, bản nháp AutoSave và toàn bộ các file media/vector/ảnh liên quan tới tệp đó.
    /// </summary>
    public static class TempCleanupManager
    {
        private static readonly object _cleanupLock = new object();

        /// <summary>
        /// Thư mục làm việc tạm duy nhất cho các Tab đang mở (cùng nơi lưu media/tệp khi tạo tab mới hoặc mở file).
        /// Khi đóng Tab, mọi tệp không còn Tab nào tham chiếu trong thư mục này sẽ bị xóa sạch ngay lập tức.
        /// </summary>
        public static string ActiveTabWorkDirectory
        {
            get
            {
                string dir = Path.Combine(Path.GetTempPath(), "ZenQOffice", "Media");
                if (!Directory.Exists(dir))
                {
                    try { Directory.CreateDirectory(dir); } catch { }
                }
                return dir;
            }
        }

        public static bool IsTempWorkFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            try
            {
                string full = Path.GetFullPath(filePath);
                string tempRoot = Path.GetFullPath(Path.GetTempPath());
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string autoSaveDir = Path.GetFullPath(Path.Combine(appData, "GenOffice", "autosave"));
                string legacyTmplDir = Path.GetFullPath(Path.Combine(appData, "GenOffice", "Templates"));

                if (full.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)) return true;
                if (full.StartsWith(autoSaveDir, StringComparison.OrdinalIgnoreCase)) return true;
                if (full.StartsWith(legacyTmplDir, StringComparison.OrdinalIgnoreCase)) return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Xóa triệt để mọi thư mục lưu trữ ngoại tuyến cũ (Templates, TemplateCache, Cache\Graphics, Cache\Images)
        /// để đảm bảo 100% Cloud không lưu bất kỳ tệp nào trong AppData của mẫu hay vector.
        /// </summary>
        public static void PurgeAllCloudCachesOnDisk()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                SafeDeleteDirectory(Path.Combine(appData, "GenOffice", "Templates"));
                SafeDeleteDirectory(Path.Combine(localAppData, "ZenQOffice", "TemplateCache"));
                SafeDeleteDirectory(Path.Combine(appData, "ZenQOffice", "Cache"));
            }
            catch { }
        }

        /// <summary>
        /// Dọn dẹp tài nguyên bộ nhớ và xóa ngay lập tức tệp mẫu tạm cùng toàn bộ các tệp liên quan của một Tab khi đóng.
        /// </summary>
        public static void CleanupTab(DocTab closedTab, List<DocTab> remainingTabs)
        {
            if (closedTab == null) return;

            lock (_cleanupLock)
            {
                try
                {
                    // 1. Lưu lại danh sách các file của tab vừa đóng trước khi giải phóng
                    HashSet<string> closedTabFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    CollectReferencedFiles(closedTab, closedTabFiles);

                    // 2. Giải phóng bộ nhớ RAM & GDI+/D2D handles của bài trình chiếu (Slides) để mở khóa toàn bộ file trên đĩa
                    if (closedTab.PresentationData != null)
                    {
                        DisposePresentation(closedTab.PresentationData);
                        closedTab.PresentationData = null;
                    }

                    // 3. Giải phóng Undo/Redo Stacks của Slides
                    if (closedTab.SlideUndoStack != null)
                    {
                        foreach (var pres in closedTab.SlideUndoStack)
                        {
                            DisposePresentation(pres);
                        }
                        closedTab.SlideUndoStack.Clear();
                        closedTab.SlideUndoStack = null;
                    }
                    if (closedTab.SlideRedoStack != null)
                    {
                        foreach (var pres in closedTab.SlideRedoStack)
                        {
                            DisposePresentation(pres);
                        }
                        closedTab.SlideRedoStack.Clear();
                        closedTab.SlideRedoStack = null;
                    }

                    // 4. Giải phóng bộ nhớ RAM của Photo & Design
                    if (closedTab.PhotoData != null)
                    {
                        try { closedTab.PhotoData.Dispose(); } catch { }
                        closedTab.PhotoData = null;
                    }

                    // 5. Giải phóng bộ nhớ RAM của Writer
                    if (closedTab.WriterShapes != null)
                    {
                        foreach (var s in closedTab.WriterShapes)
                        {
                            try { s.CachedImage?.Dispose(); s.CachedImage = null; } catch { }
                        }
                        closedTab.WriterShapes.Clear();
                    }

                    // 6. Thu thập danh sách tệp đang sử dụng bởi các Tab khác rồi đẩy dọn dẹp đĩa sang luồng nền để không làm đơ UI
                    HashSet<string> activeMediaFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    HashSet<string> activeDraftNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    int openDocTabsCount = 0;

                    if (remainingTabs != null)
                    {
                        foreach (var tab in remainingTabs)
                        {
                            if (tab == null || ReferenceEquals(tab, closedTab) || tab.Mode == OfficeAppMode.Home) continue;
                            openDocTabsCount++;
                            CollectReferencedFiles(tab, activeMediaFiles);
                            if (!string.IsNullOrEmpty(tab.DraftFile))
                            {
                                activeDraftNames.Add(tab.DraftFile);
                            }
                        }
                    }

                    string closedTempSource = closedTab.TempSourceFile;
                    string closedFilePath = closedTab.FilePath;

                    System.Threading.Tasks.Task.Run(() =>
                    {
                        lock (_cleanupLock)
                        {
                            try
                            {
                                bool anyDeleteFailed = false;
                                Action<string> tryDelete = (path) =>
                                {
                                    if (string.IsNullOrEmpty(path)) return;
                                    try
                                    {
                                        if (File.Exists(path)) File.Delete(path);
                                    }
                                    catch
                                    {
                                        anyDeleteFailed = true;
                                    }
                                };

                                if (!string.IsNullOrEmpty(closedTempSource) && !activeMediaFiles.Contains(closedTempSource))
                                {
                                    tryDelete(closedTempSource);
                                }
                                if (!string.IsNullOrEmpty(closedFilePath) && IsTempWorkFile(closedFilePath) && !activeMediaFiles.Contains(closedFilePath))
                                {
                                    tryDelete(closedFilePath);
                                }

                                foreach (var f in closedTabFiles)
                                {
                                    if (!string.IsNullOrEmpty(f) && IsTempWorkFile(f) && !activeMediaFiles.Contains(f))
                                    {
                                        tryDelete(f);
                                    }
                                }

                                if (anyDeleteFailed)
                                {
                                    // Chỉ chạy GC ở luồng nền khi thực sự có tệp đang bị handle GDI+ giữ khóa
                                    GC.Collect();
                                    GC.WaitForPendingFinalizers();
                                    if (!string.IsNullOrEmpty(closedTempSource) && !activeMediaFiles.Contains(closedTempSource))
                                    {
                                        try { if (File.Exists(closedTempSource)) File.Delete(closedTempSource); } catch { }
                                    }
                                    if (!string.IsNullOrEmpty(closedFilePath) && IsTempWorkFile(closedFilePath) && !activeMediaFiles.Contains(closedFilePath))
                                    {
                                        try { if (File.Exists(closedFilePath)) File.Delete(closedFilePath); } catch { }
                                    }
                                    foreach (var f in closedTabFiles)
                                    {
                                        if (!string.IsNullOrEmpty(f) && IsTempWorkFile(f) && !activeMediaFiles.Contains(f))
                                        {
                                            try { if (File.Exists(f)) File.Delete(f); } catch { }
                                        }
                                    }
                                }

                                if (openDocTabsCount == 0)
                                {
                                    CleanupUnreferencedTempMedia(activeMediaFiles);
                                }

                                CleanupAutoSaveDirectory(activeDraftNames, openDocTabsCount == 0);

                                if (openDocTabsCount == 0)
                                {
                                    string tempBase = Path.GetTempPath();
                                    SafeDeleteDirectory(Path.Combine(tempBase, "ZenQOffice", "Media"));
                                    SafeDeleteDirectory(Path.Combine(tempBase, "GenOffice_Media"));
                                    SafeDeleteDirectory(Path.Combine(tempBase, "GenOffice_Fonts"));
                                }

                                PurgeAllCloudCachesOnDisk();
                            }
                            catch { }
                        }
                    });
                }
                catch { }
            }
        }

        /// <summary>
        /// Dọn dẹp các file tạm không thuộc về bất kỳ Tab đang mở nào (dùng sau khi render tạm hoặc đóng hộp thoại).
        /// </summary>
        public static void CleanupUnreferencedByActiveTabs(AppForm appForm = null)
        {
            lock (_cleanupLock)
            {
                try
                {
                    HashSet<string> activeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    AppForm form = appForm;
                    if (form == null)
                    {
                        foreach (System.Windows.Forms.Form f in System.Windows.Forms.Application.OpenForms)
                        {
                            if (f is AppForm af) { form = af; break; }
                        }
                    }
                    if (form != null && form.Ribbon != null && form.Ribbon.DocTabs != null)
                    {
                        foreach (var tab in form.Ribbon.DocTabs)
                        {
                            if (tab != null && tab.Mode != OfficeAppMode.Home)
                            {
                                CollectReferencedFiles(tab, activeFiles);
                            }
                        }
                        if (form.SlidesView != null && form.SlidesView.Presentation != null)
                        {
                            CollectPresentationFiles(form.SlidesView.Presentation, activeFiles);
                        }
                    }
                    CleanupUnreferencedTempMedia(activeFiles);
                }
                catch { }
            }
        }

        /// <summary>
        /// Thu thập toàn bộ các file media, audio, video, ảnh, vector, template tạm mà một tab đang tham chiếu.
        /// </summary>
        public static void CollectReferencedFiles(DocTab tab, HashSet<string> activeFiles)
        {
            if (tab == null || activeFiles == null) return;
            try
            {
                if (!string.IsNullOrEmpty(tab.FilePath)) activeFiles.Add(tab.FilePath);
                if (!string.IsNullOrEmpty(tab.TempSourceFile)) activeFiles.Add(tab.TempSourceFile);
                if (!string.IsNullOrEmpty(tab.DraftFile))
                {
                    activeFiles.Add(tab.DraftFile);
                    string autoSaveDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenOffice", "autosave");
                    activeFiles.Add(Path.Combine(autoSaveDir, tab.DraftFile));
                }

                if (tab.PresentationData != null)
                {
                    CollectPresentationFiles(tab.PresentationData, activeFiles);
                }

                if (tab.SlideUndoStack != null)
                {
                    foreach (var pres in tab.SlideUndoStack) CollectPresentationFiles(pres, activeFiles);
                }
                if (tab.SlideRedoStack != null)
                {
                    foreach (var pres in tab.SlideRedoStack) CollectPresentationFiles(pres, activeFiles);
                }

                if (tab.WriterShapes != null)
                {
                    foreach (var shape in tab.WriterShapes)
                    {
                        if (!string.IsNullOrEmpty(shape?.ImagePath)) activeFiles.Add(shape.ImagePath);
                    }
                }
            }
            catch { }
        }

        public static void CollectPresentationFiles(PresentationRoot pres, HashSet<string> activeFiles)
        {
            if (pres == null || pres.Slides == null || activeFiles == null) return;
            try
            {
                foreach (var slide in pres.Slides)
                {
                    if (slide == null) continue;
                    if (!string.IsNullOrEmpty(slide.TransitionSound) && File.Exists(slide.TransitionSound))
                    {
                        activeFiles.Add(slide.TransitionSound);
                    }

                    if (slide.Shapes == null) continue;
                    foreach (var shape in slide.Shapes)
                    {
                        if (shape == null) continue;
                        if (!string.IsNullOrEmpty(shape.ImagePath)) activeFiles.Add(shape.ImagePath);
                        if (!string.IsNullOrEmpty(shape.VideoPath)) activeFiles.Add(shape.VideoPath);
                        if (!string.IsNullOrEmpty(shape.AudioPath)) activeFiles.Add(shape.AudioPath);
                        if (!string.IsNullOrEmpty(shape.VideoPosterFramePath)) activeFiles.Add(shape.VideoPosterFramePath);
                        if (!string.IsNullOrEmpty(shape.VideoCaptionPath)) activeFiles.Add(shape.VideoCaptionPath);
                        if (!string.IsNullOrEmpty(shape.ClickCustomSoundPath)) activeFiles.Add(shape.ClickCustomSoundPath);
                        if (!string.IsNullOrEmpty(shape.HoverCustomSoundPath)) activeFiles.Add(shape.HoverCustomSoundPath);
                    }
                }
            }
            catch { }
        }

        private static void DisposePresentation(PresentationRoot pres)
        {
            if (pres == null || pres.Slides == null) return;
            try
            {
                foreach (var slide in pres.Slides)
                {
                    if (slide == null) continue;
                    try { slide.ThumbnailCache?.Dispose(); slide.ThumbnailCache = null; } catch { }
                    try { slide.NativeThumbnail?.Dispose(); slide.NativeThumbnail = null; } catch { }

                    if (slide.Shapes == null) continue;
                    foreach (var shape in slide.Shapes)
                    {
                        if (shape == null) continue;
                        try { shape.CachedImage?.Dispose(); shape.CachedImage = null; } catch { }
                        try { shape.DisplayCachedImage?.Dispose(); shape.DisplayCachedImage = null; } catch { }
                        try { shape.CachedFeatheredBmp?.Dispose(); shape.CachedFeatheredBmp = null; } catch { }
                        try { shape.CachedShadowBmp?.Dispose(); shape.CachedShadowBmp = null; } catch { }
                        try { shape.NativeD2DBitmap?.Dispose(); shape.NativeD2DBitmap = null; } catch { }
                    }
                }
            }
            catch { }
        }

        private static void CleanupUnreferencedTempMedia(HashSet<string> activeFiles)
        {
            try
            {
                string[] tempMediaDirs = new string[]
                {
                    Path.Combine(Path.GetTempPath(), "ZenQOffice", "Media"),
                    Path.Combine(Path.GetTempPath(), "GenOffice_Media")
                };

                foreach (var dir in tempMediaDirs)
                {
                    if (!Directory.Exists(dir)) continue;
                    var files = Directory.GetFiles(dir);
                    foreach (var file in files)
                    {
                        if (activeFiles == null || !activeFiles.Contains(file))
                        {
                            try { File.Delete(file); } catch { }
                        }
                    }
                }
            }
            catch { }
        }

        private static void CleanupAutoSaveDirectory(HashSet<string> activeDraftNames, bool deleteAllDrafts)
        {
            try
            {
                string autoSaveDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenOffice", "autosave");
                if (!Directory.Exists(autoSaveDir)) return;

                foreach (var file in Directory.GetFiles(autoSaveDir))
                {
                    string fname = Path.GetFileName(file);
                    if (fname.StartsWith("draft_", StringComparison.OrdinalIgnoreCase) ||
                        fname.StartsWith("autosave_", StringComparison.OrdinalIgnoreCase))
                    {
                        if (deleteAllDrafts || activeDraftNames == null || !activeDraftNames.Contains(fname))
                        {
                            try { File.Delete(file); } catch { }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Giải phóng và xóa riêng các file tạm của một Presentation tạm thời (ví dụ khi dựng preview thumbnail),
        /// tuyệt đối KHÔNG chạm vào bất kỳ file nào của các Tab đang mở.
        /// </summary>
        public static void DisposeAndDeletePresentationTempMedia(PresentationRoot pres, string tempPptxPath = null)
        {
            lock (_cleanupLock)
            {
                try
                {
                    HashSet<string> previewFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (!string.IsNullOrEmpty(tempPptxPath)) previewFiles.Add(tempPptxPath);
                    if (pres != null)
                    {
                        CollectPresentationFiles(pres, previewFiles);
                        DisposePresentation(pres);
                    }
                    foreach (var f in previewFiles)
                    {
                        if (!string.IsNullOrEmpty(f) && IsTempWorkFile(f))
                        {
                            try { if (File.Exists(f)) File.Delete(f); } catch { }
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Khi đóng ứng dụng: tuyệt đối KHÔNG xóa dữ liệu hay tệp của các Tab đang mở (chỉ xóa khi người dùng bấm đóng Tab).
        /// Chỉ dọn các thư mục Cache ngoại tuyến cấm lưu trong AppData.
        /// </summary>
        public static void CleanupOnAppExit(AppForm appForm = null)
        {
            lock (_cleanupLock)
            {
                try
                {
                    PurgeAllCloudCachesOnDisk();
                }
                catch { }
            }
        }

        private static void CleanStrayTempFiles(string tempBase, int olderThanHours)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(tempBase);
                DateTime threshold = DateTime.Now.AddHours(-olderThanHours);

                string[] patterns = new string[] { "genoffice_*.*", "ScreenClip_*.*", "SlideScreenClip_*.*" };
                foreach (var pat in patterns)
                {
                    foreach (var fi in di.GetFiles(pat))
                    {
                        if (olderThanHours <= 0 || fi.LastWriteTime < threshold)
                        {
                            try { fi.Delete(); } catch { }
                        }
                    }
                }
            }
            catch { }
        }

        public static void SafeDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch { }
        }
    }
}

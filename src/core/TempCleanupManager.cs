using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using ZenQOffice.UI;

namespace ZenQOffice.Core
{
    /// <summary>
    /// Trình quản lý giải phóng bộ nhớ và dọn dẹp bộ nhớ đệm / tệp tạm thời.
    /// Đảm bảo không bao giờ tích tụ hàng GB tệp rác (media, audio, video, svg, template pptx) trên ổ cứng.
    /// </summary>
    public static class TempCleanupManager
    {
        private static readonly object _cleanupLock = new object();

        /// <summary>
        /// Dọn dẹp tài nguyên bộ nhớ và các tệp đa phương tiện tạm của một Tab khi đóng.
        /// </summary>
        public static void CleanupTab(DocTab closedTab, List<DocTab> remainingTabs)
        {
            if (closedTab == null) return;

            try
            {
                // 1. Giải phóng bộ nhớ RAM của bài trình chiếu (Slides)
                if (closedTab.PresentationData != null)
                {
                    DisposePresentation(closedTab.PresentationData);
                    closedTab.PresentationData = null;
                }

                // 2. Giải phóng Undo/Redo Stacks của Slides (vốn giữ bản sao toàn bộ Presentation)
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

                // 3. Giải phóng bộ nhớ RAM của Photo & Design
                if (closedTab.PhotoData != null)
                {
                    try { closedTab.PhotoData.Dispose(); } catch { }
                    closedTab.PhotoData = null;
                }

                // 4. Giải phóng bộ nhớ RAM của Writer
                if (closedTab.WriterShapes != null)
                {
                    foreach (var s in closedTab.WriterShapes)
                    {
                        try { s.CachedImage?.Dispose(); s.CachedImage = null; } catch { }
                    }
                    closedTab.WriterShapes.Clear();
                }

                // 5. Thu thập danh sách các tệp Media còn đang được sử dụng bởi các Tab còn mở
                HashSet<string> activeMediaFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (remainingTabs != null)
                {
                    foreach (var tab in remainingTabs)
                    {
                        if (tab == null) continue;
                        CollectReferencedMediaFiles(tab, activeMediaFiles);
                    }
                }

                // 6. Xóa các tệp Media tạm thời trong %TEMP%\ZenQOffice\Media không còn tab nào dùng
                CleanupUnreferencedTempMedia(activeMediaFiles);

                // 7. Kích hoạt Garbage Collector giải phóng RAM ngay lập tức
                GC.Collect(2, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
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
                    if (slide == null || slide.Shapes == null) continue;
                    foreach (var shape in slide.Shapes)
                    {
                        if (shape == null) continue;
                        try { shape.CachedImage?.Dispose(); shape.CachedImage = null; } catch { }
                        try { shape.CachedShadowBmp?.Dispose(); shape.CachedShadowBmp = null; } catch { }
                        try { shape.NativeD2DBitmap?.Dispose(); shape.NativeD2DBitmap = null; } catch { }
                    }
                }
            }
            catch { }
        }

        private static void CollectReferencedMediaFiles(DocTab tab, HashSet<string> activeFiles)
        {
            if (tab == null || activeFiles == null) return;
            try
            {
                if (tab.PresentationData != null && tab.PresentationData.Slides != null)
                {
                    foreach (var slide in tab.PresentationData.Slides)
                    {
                        if (slide?.Shapes == null) continue;
                        foreach (var shape in slide.Shapes)
                        {
                            if (!string.IsNullOrEmpty(shape?.ImagePath))
                            {
                                activeFiles.Add(shape.ImagePath);
                            }
                        }
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
                        if (!activeFiles.Contains(file))
                        {
                            try { File.Delete(file); } catch { }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Dọn dẹp toàn bộ bộ nhớ đệm, tệp âm thanh, video, ảnh trích xuất và mẫu slide tạm khi đóng ứng dụng.
        /// Giải phóng 100% dung lượng rác đã tích lũy trong phiên làm việc.
        /// </summary>
        public static void CleanupOnAppExit()
        {
            lock (_cleanupLock)
            {
                try
                {
                    // A. Dọn dẹp cache mẫu slide tải từ GitHub
                    OnlineSlideTemplateManager.CleanupCache();

                    // B. Dọn dẹp cache đồ họa vector SVG và ảnh trực tuyến
                    OnlineGraphicsManager.CleanupCache();
                    OnlineImagesManager.CleanupCache();

                    // C. Xóa sạch thư mục Media tạm (%TEMP%\ZenQOffice\Media và GenOffice_Media)
                    string tempBase = Path.GetTempPath();
                    SafeDeleteDirectory(Path.Combine(tempBase, "ZenQOffice", "Media"));
                    SafeDeleteDirectory(Path.Combine(tempBase, "GenOffice_Media"));
                    SafeDeleteDirectory(Path.Combine(tempBase, "ZenQOffice_Scans"));
                    SafeDeleteDirectory(Path.Combine(tempBase, "ZenQOffice_MailAttachments"));
                    SafeDeleteDirectory(Path.Combine(tempBase, "GenOffice_Fonts"));

                    // D. Xóa các file ảnh tạm chụp màn hình hoặc trích xuất tạm trong %TEMP%
                    try
                    {
                        DirectoryInfo di = new DirectoryInfo(tempBase);
                        foreach (var fi in di.GetFiles("genoffice_*.*")) { try { fi.Delete(); } catch { } }
                        foreach (var fi in di.GetFiles("ScreenClip_*.*")) { try { fi.Delete(); } catch { } }
                        foreach (var fi in di.GetFiles("SlideScreenClip_*.*")) { try { fi.Delete(); } catch { } }
                    }
                    catch { }

                    GC.Collect();
                }
                catch { }
            }
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

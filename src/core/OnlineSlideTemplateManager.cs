using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ZenQOffice.Core;
using ZenQOffice.UI;

namespace ZenQOffice.Core
{
    public class SlideTemplateItem
    {
        public string id { get; set; }
        public string title { get; set; }
        public string subtitle { get; set; }
        public string category { get; set; }
        public List<string> tags { get; set; } = new List<string>();
        public int slideCount { get; set; } = 1;
        public string accentColor { get; set; } = "#6366F1";
        public bool isCustom { get; set; } = false;
        public string pptxUrl { get; set; } = "";
        public string localPptxPath { get; set; } = "";
    }

    public static class OnlineSlideTemplateManager
    {
        private static readonly Dictionary<string, int> _templateUsageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static string UsageDataPath => Path.Combine(UserTemplatesDirectory, "template_usage.txt");

        public static void RecordTemplateUsage(string templateId)
        {
            if (string.IsNullOrEmpty(templateId)) return;
            lock (_syncLock)
            {
                if (_templateUsageCounts.ContainsKey(templateId))
                    _templateUsageCounts[templateId]++;
                else
                    _templateUsageCounts[templateId] = 1;

                SaveUsageCounts();
            }
        }

        public static int GetUsageCount(string templateId)
        {
            if (string.IsNullOrEmpty(templateId)) return 0;
            lock (_syncLock)
            {
                return _templateUsageCounts.TryGetValue(templateId, out int cnt) ? cnt : 0;
            }
        }

        private static void LoadUsageCounts()
        {
            try
            {
                if (File.Exists(UsageDataPath))
                {
                    var lines = File.ReadAllLines(UsageDataPath);
                    foreach (var l in lines)
                    {
                        var parts = l.Split('	');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int c))
                        {
                            _templateUsageCounts[parts[0]] = c;
                        }
                    }
                }
            }
            catch { }
        }

        private static void SaveUsageCounts()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                foreach (var kvp in _templateUsageCounts)
                {
                    sb.AppendLine($"{kvp.Key}	{kvp.Value}");
                }
                File.WriteAllText(UsageDataPath, sb.ToString());
            }
            catch { }
        }

        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        private static readonly object _syncLock = new object();
        private static readonly object _renderLock = new object();
        private static readonly Dictionary<string, Bitmap> _bitmapCache = new Dictionary<string, Bitmap>();
        private static readonly Dictionary<string, (DateTime LoadedTime, PresentationRoot Pres)> _presCache = new Dictionary<string, (DateTime, PresentationRoot)>();
        private static readonly Queue<(string TemplateId, int SlideIndex)> _renderQueue = new Queue<(string, int)>();
        private static readonly HashSet<string> _queuedOrDone = new HashSet<string>();
        private static bool _isWorkerRunning = false;
        private static bool _initialized = false;

        public static event Action OnSlideRendered;

        // Cloud Slide Templates Repository: danhtruong6112009-source/zenq-slide-templates
        public const string GITHUB_CONTENTS_API = "https://api.github.com/repos/danhtruong6112009-source/zenq-slide-templates/contents/templates";
        public const string GITHUB_ROOT_CONTENTS_API = "https://api.github.com/repos/danhtruong6112009-source/zenq-slide-templates/contents";

        public static List<SlideTemplateItem> Templates { get; private set; } = new List<SlideTemplateItem>();

        public static string CacheDirectory
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZenQOffice", "TemplateCache");
                if (!Directory.Exists(dir))
                {
                    try { Directory.CreateDirectory(dir); } catch { }
                }
                return dir;
            }
        }

        public static string UserTemplatesDirectory
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenOffice", "Templates");
                if (!Directory.Exists(dir))
                {
                    try { Directory.CreateDirectory(dir); } catch { }
                }
                return dir;
            }
        }

        public static void Initialize()
        {
            if (_initialized) return;
            lock (_syncLock)
            {
                if (_initialized) return;
                _initialized = true;

                // 1. Khởi tạo danh sách mẫu rỗng (Tuyệt đối không tạo mẫu ảo)
                LoadUsageCounts();
                LoadBuiltinTemplates();

                // 2. Chạy tác vụ nền: Đồng bộ các tệp PPTX thực tế từ GitHub Cloud
                Task.Run(async () =>
                {
                    try
                    {
                        await RefreshCatalogFromGitHubAsync();
                    }
                    catch { }
                });
            }
        }

        private static PresentationRoot GetOrLoadPresentation(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            lock (_syncLock)
            {
                if (_presCache.TryGetValue(path, out var entry) && (DateTime.UtcNow - entry.LoadedTime).TotalMinutes < 10)
                {
                    return entry.Pres;
                }
            }
            try
            {
                var pres = ZenQOffice.Format.PptxHandler.LoadPptx(path);
                if (pres != null)
                {
                    lock (_syncLock)
                    {
                        _presCache[path] = (DateTime.UtcNow, pres);
                    }
                }
                return pres;
            }
            catch
            {
                return null;
            }
        }

        private static string CustomTemplatesJsonPath => Path.Combine(UserTemplatesDirectory, "custom_templates.json");

        private static void SaveCustomTemplates()
        {
            try
            {
                List<SlideTemplateItem> customList = new List<SlideTemplateItem>();
                lock (_syncLock)
                {
                    foreach (var t in Templates)
                    {
                        if (t != null && t.isCustom && !string.IsNullOrEmpty(t.localPptxPath) && File.Exists(t.localPptxPath))
                        {
                            customList.Add(t);
                        }
                    }
                }
                string json = JsonSerializer.Serialize(customList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CustomTemplatesJsonPath, json);
            }
            catch { }
        }

        private static void LoadCustomTemplates()
        {
            try
            {
                if (File.Exists(CustomTemplatesJsonPath))
                {
                    string json = File.ReadAllText(CustomTemplatesJsonPath);
                    var items = JsonSerializer.Deserialize<List<SlideTemplateItem>>(json);
                    if (items != null)
                    {
                        foreach (var it in items)
                        {
                            if (it == null) continue;
                            it.isCustom = true;
                            if (!File.Exists(it.localPptxPath))
                            {
                                string fn = Path.GetFileName(it.localPptxPath);
                                string fallback = Path.Combine(UserTemplatesDirectory, fn);
                                if (File.Exists(fallback)) it.localPptxPath = fallback;
                            }
                            if (File.Exists(it.localPptxPath))
                            {
                                if (!Templates.Exists(x => x.id == it.id))
                                {
                                    Templates.Add(it);
                                }
                            }
                        }
                    }
                }

                if (Directory.Exists(UserTemplatesDirectory))
                {
                    var files = Directory.GetFiles(UserTemplatesDirectory, "*.pptx");
                    foreach (var f in files)
                    {
                        string fname = Path.GetFileName(f);
                        if (fname.StartsWith("~$")) continue;
                        if (!Templates.Exists(x => string.Equals(x.localPptxPath, f, StringComparison.OrdinalIgnoreCase)))
                        {
                            var pres = GetOrLoadPresentation(f);
                            string title = Path.GetFileNameWithoutExtension(f);
                            int sCount = 1;
                            if (pres != null && pres.Slides != null && pres.Slides.Count > 0)
                            {
                                sCount = pres.Slides.Count;
                                if (!string.IsNullOrEmpty(pres.Slides[0].Title)) title = pres.Slides[0].Title.Trim();
                            }
                            var it = new SlideTemplateItem
                            {
                                id = "custom_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                                title = title,
                                subtitle = "",
                                category = "Cá nhân",
                                tags = new List<string> { "Đã tải lên" },
                                slideCount = sCount,
                                accentColor = "#3B82F6",
                                isCustom = true,
                                localPptxPath = f
                            };
                            Templates.Add(it);
                        }
                    }
                }
            }
            catch { }
        }

        private static void LoadBuiltinTemplates()
        {
            Templates = new List<SlideTemplateItem>();
            // Nạp và khôi phục các mẫu slide do người dùng đã tải lên
            LoadCustomTemplates();
        }

        public static async Task RefreshCatalogFromGitHubAsync()
        {
            // Tự động quét và phát hiện các file .pptx thực tế tải lên kho GitHub (danhtruong6112009-source/zenq-slide-templates)
            try
            {
                await ScanGitHubPptxFilesAsync(GITHUB_CONTENTS_API).ConfigureAwait(false);
                await ScanGitHubPptxFilesAsync(GITHUB_ROOT_CONTENTS_API).ConfigureAwait(false);
            }
            catch { }

            try
            {
                OnSlideRendered?.Invoke();
            }
            catch { }
        }

        private static async Task ScanGitHubPptxFilesAsync(string apiEndpoint)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, apiEndpoint);
                req.Headers.UserAgent.ParseAdd("ZenQOffice/1.0");
                var resp = await _httpClient.SendAsync(req).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return;

                string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(json)) return;

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return;

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out var nameProp) && item.TryGetProperty("type", out var typeProp))
                    {
                        string name = nameProp.GetString() ?? "";
                        string type = typeProp.GetString() ?? "";
                        if (type == "file" && (name.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".zenq", StringComparison.OrdinalIgnoreCase)))
                        {
                            string downloadUrl = item.TryGetProperty("download_url", out var dlProp) ? (dlProp.GetString() ?? "") : "";
                            if (!string.IsNullOrEmpty(downloadUrl))
                            {
                                string tmplId = "cloud_" + Path.GetFileNameWithoutExtension(name).ToLowerInvariant().Replace(" ", "_");
                                bool alreadyExists = false;
                                lock (_syncLock)
                                {
                                    alreadyExists = Templates.Exists(t => t.id == tmplId || t.title.Equals(Path.GetFileNameWithoutExtension(name), StringComparison.OrdinalIgnoreCase));
                                }

                                if (!alreadyExists)
                                {
                                    // Tải tạm theo nhu cầu (On-Demand): KHÔNG tải ngầm toàn bộ PPTX từ GitHub về đĩa.
                                    // Chỉ lưu thông tin metadata, khi nào người dùng bấm vào xem/chọn mẫu mới tải về.
                                    string displayTitle = Path.GetFileNameWithoutExtension(name).Replace("_", " ");
                                    var cloudTmpl = new SlideTemplateItem
                                    {
                                        id = tmplId,
                                        title = displayTitle,
                                        subtitle = "",
                                        category = "Thuyết trình",
                                        tags = new List<string> { "Cloud", "GitHub" },
                                        slideCount = 1,
                                        accentColor = "#3B82F6",
                                        localPptxPath = "",
                                        pptxUrl = downloadUrl,
                                        isCustom = false
                                    };

                                    lock (_syncLock)
                                    {
                                        Templates.Insert(0, cloudTmpl);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public static Bitmap GetSlideBitmap(string templateId, int slideIndex)
        {
            string cacheKey = $"{templateId}_{slideIndex}";
            lock (_syncLock)
            {
                if (_bitmapCache.TryGetValue(cacheKey, out Bitmap bmp) && bmp != null)
                {
                    return bmp;
                }
            }

            string filePath = Path.Combine(CacheDirectory, $"{cacheKey}.png");
            if (File.Exists(filePath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        Bitmap loaded = new Bitmap(Image.FromStream(ms));
                        lock (_syncLock)
                        {
                            _bitmapCache[cacheKey] = loaded;
                        }
                        return loaded;
                    }
                }
                catch { }
            }

            // Chưa có trong cache -> Đẩy vào hàng đợi xử lý nền từ từ
            QueueSlideRender(templateId, slideIndex);
            return null;
        }

        public static void QueueSlideRender(string templateId, int slideIndex)
        {
            string key = $"{templateId}_{slideIndex}";
            lock (_syncLock)
            {
                if (_queuedOrDone.Contains(key)) return;
                _queuedOrDone.Add(key);
                _renderQueue.Enqueue((templateId, slideIndex));

                if (!_isWorkerRunning)
                {
                    _isWorkerRunning = true;
                    Task.Run(ProcessRenderQueueAsync);
                }
            }
        }

        private static async Task ProcessRenderQueueAsync()
        {
            while (true)
            {
                (string templateId, int slideIndex) item;
                lock (_syncLock)
                {
                    if (_renderQueue.Count == 0)
                    {
                        _isWorkerRunning = false;
                        return;
                    }
                    item = _renderQueue.Dequeue();
                }

                try
                {
                    await Task.Delay(80).ConfigureAwait(false);

                    SlideTemplateItem tmpl = null;
                    lock (_syncLock)
                    {
                        tmpl = Templates.Find(t => t.id == item.templateId);
                    }

                    if (tmpl != null)
                    {
                        Bitmap rendered = null;
                        lock (_renderLock)
                        {
                            rendered = RenderSlideToBitmap(tmpl, item.slideIndex);
                        }

                        if (rendered != null)
                        {
                            string key = $"{item.templateId}_{item.slideIndex}";
                            string filePath = Path.Combine(CacheDirectory, $"{key}.png");
                            try
                            {
                                rendered.Save(filePath, ImageFormat.Png);
                            }
                            catch { }

                            lock (_syncLock)
                            {
                                _bitmapCache[key] = rendered;
                            }

                            try
                            {
                                OnSlideRendered?.Invoke();
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ProcessRenderQueue error: {ex.Message}");
                }
            }
        }

        public static Bitmap RenderPptxSlideToBitmap(string pptxPath, int slideIndex, int targetW = 960, int targetH = 540)
        {
            if (string.IsNullOrEmpty(pptxPath) || !File.Exists(pptxPath)) return null;
            try
            {
                var pres = GetOrLoadPresentation(pptxPath);
                if (pres != null && pres.Slides != null && pres.Slides.Count > 0)
                {
                    int idx = Math.Max(0, Math.Min(slideIndex, pres.Slides.Count - 1));
                    var slide = pres.Slides[idx];
                    var sv = new ZenQOffice.UI.SlidesView();
                    sv.Presentation = pres;
                    sv.ActiveSlideIndex = idx;
                    return sv.RenderSlideToBitmap(slide, targetW, targetH);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RenderPptxSlideToBitmap error: {ex.Message}");
            }
            return null;
        }

        public static Bitmap RenderSlideToBitmap(SlideTemplateItem tmpl, int slideIndex, int targetW = 960, int targetH = 540)
        {
            if (tmpl == null) return null;

            string pptx = EnsureTemplatePptx(tmpl);
            if (!string.IsNullOrEmpty(pptx) && File.Exists(pptx))
            {
                Bitmap pptxBmp = RenderPptxSlideToBitmap(pptx, slideIndex, targetW, targetH);
                if (pptxBmp != null) return pptxBmp;
            }

            return null;
        }

        public static string EnsureTemplatePptx(SlideTemplateItem tmpl)
        {
            if (tmpl == null) return null;

            if (!string.IsNullOrEmpty(tmpl.localPptxPath) && File.Exists(tmpl.localPptxPath))
            {
                RecordTemplateUsage(tmpl.id);
                return tmpl.localPptxPath;
            }

            // Nếu template có link tải trực tiếp từ Cloud
            if (!string.IsNullOrEmpty(tmpl.pptxUrl))
            {
                try
                {
                    string safeName = CleanFileName(tmpl.title);
                    string userPptxPath = Path.Combine(UserTemplatesDirectory, $"{safeName}.pptx");
                    using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) })
                    {
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("ZenQOffice/1.0");
                        var data = client.GetByteArrayAsync(tmpl.pptxUrl).GetAwaiter().GetResult();
                        if (data != null && data.Length > 0)
                        {
                            File.WriteAllBytes(userPptxPath, data);
                            tmpl.localPptxPath = userPptxPath;
                            RecordTemplateUsage(tmpl.id);
                            return userPptxPath;
                        }
                    }
                }
                catch { }
            }

            return null;
        }

        private static string CleanFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Template";
            char[] invalid = Path.GetInvalidFileNameChars();
            char[] chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (chars[i] == invalid[j]) { chars[i] = '_'; break; }
                }
            }
            return new string(chars).Trim().Replace(" ", "_");
        }

        public static SlideTemplateItem AddCustomTemplateFromPptx(string pptxPath)
        {
            if (!File.Exists(pptxPath)) return null;

            string name = Path.GetFileNameWithoutExtension(pptxPath);
            string id = "custom_" + Guid.NewGuid().ToString("N").Substring(0, 8);

            int slideCount = 1;
            var pres = GetOrLoadPresentation(pptxPath);
            if (pres != null && pres.Slides != null && pres.Slides.Count > 0)
            {
                slideCount = pres.Slides.Count;
                if (!string.IsNullOrEmpty(pres.Slides[0].Title) && pres.Slides[0].Title.Length < 60)
                {
                    name = pres.Slides[0].Title.Trim();
                }
            }

            // Lưu bản sao an toàn vào UserTemplatesDirectory để không bao giờ bị mất khi thoát app
            string userDir = UserTemplatesDirectory;
            string destFileName = CleanFileName(name) + "_" + id.Substring(7) + ".pptx";
            string savedPath = Path.Combine(userDir, destFileName);
            try
            {
                File.Copy(pptxPath, savedPath, true);
            }
            catch
            {
                savedPath = pptxPath;
            }

            SlideTemplateItem custom = new SlideTemplateItem
            {
                id = id,
                title = name,
                subtitle = "",
                category = "Cá nhân",
                tags = new List<string> { "Đã tải lên" },
                slideCount = slideCount,
                accentColor = "#3B82F6",
                isCustom = true,
                localPptxPath = savedPath
            };

            lock (_syncLock)
            {
                Templates.Insert(0, custom);
                SaveCustomTemplates();
            }

            QueueSlideRender(custom.id, 0);
            return custom;
        }

        /// <summary>
        /// Xóa sạch thư mục cache mẫu slide tạm tải từ GitHub và giải phóng bộ nhớ.
        /// </summary>
        public static void CleanupCache()
        {
            lock (_syncLock)
            {
                foreach (var bmp in _bitmapCache.Values)
                {
                    try { bmp?.Dispose(); } catch { }
                }
                _bitmapCache.Clear();
                _presCache.Clear();
            }
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZenQOffice", "TemplateCache");
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch { }
        }
    }
}
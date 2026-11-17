using System;

using System.Collections.Generic;

using System.Drawing;

using System.Drawing.Drawing2D;

using System.Drawing.Imaging;

using System.IO;

using System.Linq;

using System.Net.Http;

using System.Net.NetworkInformation;

using System.Runtime.InteropServices;

using System.Text;

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
        public int authorId { get; set; } = 0;
        public string authorName { get; set; } = "";

    }



    /// <summary>

    /// Quản lý Kho Mẫu Thuyết Trình 100% Cloud-Only từ GitHub (danhtruong6112009-source/zenq-slide-templates):

    /// - Tuyệt đối KHÔNG tạo hay lưu thư mục Templates hoặc TemplateCache trong AppData.

    /// - Khi duyệt mẫu: chỉ giữ metadata và ảnh thu nhỏ trang chiếu trong RAM (_bitmapCache).

    /// - Khi mất kết nối mạng: xóa danh sách RAM và báo trạng thái IsOffline = true ("Mất kết nối Internet").

    /// - Chỉ khi người dùng bấm chọn sử dụng mẫu thì mới tải tệp .pptx của mẫu đó về thư mục làm việc tạm của tab

    ///   (TempCleanupManager.ActiveTabWorkDirectory), và khi đóng tab thì xóa sạch ngay lập tức.

    /// </summary>

    public static class OnlineSlideTemplateManager

    {

        private static readonly Dictionary<string, int> _templateUsageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        private static readonly object _syncLock = new object();

        private static readonly object _renderLock = new object();

        private const int MAX_BITMAP_CACHE = 240; // Giữ thoải mái thumbnail 32bppPArgb đã thu gọn trong RAM, tránh evict khi cuộn nhanh

        private static readonly Dictionary<string, Bitmap> _bitmapCache = new Dictionary<string, Bitmap>();

        private static readonly LinkedList<string> _lruKeys = new LinkedList<string>();

        private static readonly Queue<(string TemplateId, int SlideIndex)> _renderQueue = new Queue<(string, int)>();

        private static readonly HashSet<string> _queuedOrDone = new HashSet<string>();



        private static void PutBitmapToCache(string key, Bitmap bmp)

        {

            if (string.IsNullOrEmpty(key) || bmp == null) return;

            lock (_syncLock)

            {

                if (_bitmapCache.TryGetValue(key, out var oldBmp))

                {

                    if (!ReferenceEquals(oldBmp, bmp))

                    {

                        try { oldBmp?.Dispose(); } catch { }

                    }

                    _lruKeys.Remove(key);

                }



                while (_bitmapCache.Count >= MAX_BITMAP_CACHE && _lruKeys.Count > 0)

                {

                    string evictKey = _lruKeys.First.Value;

                    _lruKeys.RemoveFirst();

                    if (_bitmapCache.Remove(evictKey, out var evictBmp))

                    {

                        _queuedOrDone.Remove(evictKey);

                        try { evictBmp?.Dispose(); } catch { }

                    }

                }



                _bitmapCache[key] = bmp;

                _lruKeys.AddLast(key);

            }

        }

        private static bool _isWorkerRunning = false;

        private static bool _initialized = false;



        public static bool IsOffline { get; private set; } = false;

        public static bool IsLoading { get; private set; } = false;



        public static event Action OnSlideRendered;



        // Cloud Slide Templates Repository: danhtruong6112009-source/zenq-slide-templates

        public const string GITHUB_CONTENTS_API = "https://api.github.com/repos/danhtruong6112009-source/zenq-slide-templates/contents/templates";

        public const string GITHUB_ROOT_CONTENTS_API = "https://api.github.com/repos/danhtruong6112009-source/zenq-slide-templates/contents";

        public const string GITHUB_CATALOG_MANIFEST_API = "https://api.github.com/repos/danhtruong6112009-source/zenq-slide-templates/contents/templates/custom_catalog.json";

        public const string GITHUB_RAW_BASE = "https://raw.githubusercontent.com/danhtruong6112009-source/zenq-slide-templates/main/templates";



        public static List<SlideTemplateItem> Templates { get; private set; } = new List<SlideTemplateItem>();



        public static void RecordTemplateUsage(string templateId)

        {

            if (string.IsNullOrEmpty(templateId)) return;

            lock (_syncLock)

            {

                if (_templateUsageCounts.ContainsKey(templateId))

                    _templateUsageCounts[templateId]++;

                else

                    _templateUsageCounts[templateId] = 1;

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



        private static long _lastNetCheckTick = 0;

        private static bool _cachedNetState = true;



        public static bool CheckInternetConnected()

        {

            long now = Environment.TickCount64;

            if (now - _lastNetCheckTick < 1500 && _lastNetCheckTick != 0)

            {

                return _cachedNetState;

            }

            try

            {

                _cachedNetState = NetworkInterface.GetIsNetworkAvailable();

            }

            catch

            {

                _cachedNetState = false;

            }

            _lastNetCheckTick = now;

            return _cachedNetState;

        }



        public static void Initialize()

        {

            if (_initialized) return;

            lock (_syncLock)

            {

                if (_initialized) return;

                _initialized = true;



                // Xóa sạch mọi thư mục cache/template cũ từng tồn tại trong AppData

                TempCleanupManager.PurgeAllCloudCachesOnDisk();

                Templates = new List<SlideTemplateItem>();



                Task.Run(async () =>

                {

                    try

                    {

                        await RefreshCatalogFromGitHubAsync().ConfigureAwait(false);

                    }

                    catch { }

                });

            }

        }



        public static string NormalizeKey(string text)

        {

            if (string.IsNullOrWhiteSpace(text)) return "";

            string noDia = TemplateUploadDialogForm.RemoveDiacritics(text).ToLowerInvariant();

            var chars = noDia.Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray();

            string s = new string(chars);

            while (s.Contains("  ")) s = s.Replace("  ", " ");

            return s.Trim();

        }



        public static bool IsSameTemplate(SlideTemplateItem a, string title, string id, string pptxUrl = null, string localPath = null)

        {

            if (a == null) return false;

            if (!string.IsNullOrEmpty(pptxUrl) && !string.IsNullOrEmpty(a.pptxUrl) &&

                string.Equals(Uri.UnescapeDataString(a.pptxUrl), Uri.UnescapeDataString(pptxUrl), StringComparison.OrdinalIgnoreCase)) return true;



            string kA = NormalizeKey(a.title);

            string kIdA = NormalizeKey(a.id);

            string kB = NormalizeKey(title);

            string kIdB = NormalizeKey(id);



            if (!string.IsNullOrEmpty(kA) && !string.IsNullOrEmpty(kB) && (kA == kB || kA.StartsWith(kB) || kB.StartsWith(kA))) return true;

            if (!string.IsNullOrEmpty(kIdA) && !string.IsNullOrEmpty(kIdB) && (kIdA == kIdB || kIdA.Replace("cloud_", "") == kIdB.Replace("cloud_", ""))) return true;



            return false;

        }



        public static void ClearMemoryForOffline()

        {

            lock (_syncLock)

            {

                IsOffline = true;

                IsLoading = false;

                Templates.Clear();

                _renderQueue.Clear();

                _queuedOrDone.Clear();

                foreach (var bmp in _bitmapCache.Values)

                {

                    try { bmp?.Dispose(); } catch { }

                }

                _bitmapCache.Clear();

                _lruKeys.Clear();

            }

        }



        public static async Task RefreshCatalogFromGitHubAsync()

        {

            TempCleanupManager.PurgeAllCloudCachesOnDisk();



            if (!CheckInternetConnected())

            {

                ClearMemoryForOffline();

                try { OnSlideRendered?.Invoke(); } catch { }

                return;

            }



            IsLoading = true;

            try { OnSlideRendered?.Invoke(); } catch { }



            try

            {

                var freshList = new List<SlideTemplateItem>();

                bool anySuccess = false;



                if (await LoadCloudCustomCatalogManifestAsync(freshList).ConfigureAwait(false))

                    anySuccess = true;



                if (await ScanGitHubPptxFilesAsync(GITHUB_CONTENTS_API, freshList).ConfigureAwait(false))

                    anySuccess = true;



                if (await ScanGitHubPptxFilesAsync(GITHUB_ROOT_CONTENTS_API, freshList).ConfigureAwait(false))

                    anySuccess = true;



                if (!anySuccess)

                {

                    ClearMemoryForOffline();

                }

                else
                {
                    // L?c b? to?n b? m?u c?a c?c ng??i d?ng ?? b? Qu?n tr? vi?n (Admin) c?m / kh?a t?i kho?n trong CSDL PostgreSQL
                    try
                    {
                        var bannedUserIds = await DatabaseAuthService.GetBannedUserIdsAsync().ConfigureAwait(false);
                        if (bannedUserIds != null && bannedUserIds.Count > 0)
                        {
                            freshList.RemoveAll(t => t.authorId > 0 && bannedUserIds.Contains(t.authorId));
                        }
                    }
                    catch { }

                    lock (_syncLock)
                    {
                        IsOffline = false;
                        IsLoading = false;
                        Templates = freshList;

                        _renderQueue.Clear();

                        // Dọn dẹp cache bitmap và queue cho các template không còn tồn tại trên đám mây

                        var activeIds = new HashSet<string>(freshList.Select(t => t.id), StringComparer.OrdinalIgnoreCase);

                        _queuedOrDone.RemoveWhere(k =>

                        {

                            int idx = k.IndexOf('_');

                            string tid = idx > 0 ? k.Substring(0, idx) : k;

                            return !activeIds.Contains(tid);

                        });

                        var toRemoveBmps = _bitmapCache.Keys.Where(k =>

                        {

                            int idx = k.IndexOf('_');

                            string tid = idx > 0 ? k.Substring(0, idx) : k;

                            return !activeIds.Contains(tid);

                        }).ToList();

                        foreach (var k in toRemoveBmps)

                        {

                            if (_bitmapCache.TryGetValue(k, out var b))

                            {

                                try { b?.Dispose(); } catch { }

                                _bitmapCache.Remove(k);

                            }

                        }

                    }

                }

            }

            catch

            {

                ClearMemoryForOffline();

            }

            finally

            {

                IsLoading = false;

                try { OnSlideRendered?.Invoke(); } catch { }

            }

        }



        private static async Task<bool> LoadCloudCustomCatalogManifestAsync(List<SlideTemplateItem> targetList)

        {

            try

            {

                string json = null;

                // 1. Ưu tiên đọc trực tiếp từ GitHub API để có dữ liệu real-time 100%, không bị dính cache CDN của raw.githubusercontent.com

                try

                {

                    using var apiReq = new HttpRequestMessage(HttpMethod.Get, GITHUB_CATALOG_MANIFEST_API);

                    apiReq.Headers.UserAgent.ParseAdd("ZenQOffice/1.0");

                    string token = GetGitHubTokenFromCredentialManager();

                    if (!string.IsNullOrWhiteSpace(token))

                    {

                        apiReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);

                    }

                    apiReq.Headers.Accept.ParseAdd("application/vnd.github.v3.raw");

                    var apiResp = await _httpClient.SendAsync(apiReq).ConfigureAwait(false);

                    if (apiResp.IsSuccessStatusCode)

                    {

                        json = await apiResp.Content.ReadAsStringAsync().ConfigureAwait(false);

                    }

                }

                catch { }



                // 2. Dự phòng: gọi raw URL kèm cache buster và Cache-Control: no-cache

                if (string.IsNullOrWhiteSpace(json))

                {

                    string cacheBuster = "?_t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    string rawUrl = $"{GITHUB_RAW_BASE}/custom_catalog.json{cacheBuster}";

                    using var req = new HttpRequestMessage(HttpMethod.Get, rawUrl);

                    req.Headers.UserAgent.ParseAdd("ZenQOffice/1.0");

                    req.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true, NoStore = true };

                    var resp = await _httpClient.SendAsync(req).ConfigureAwait(false);

                    if (resp.IsSuccessStatusCode)

                    {

                        json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                    }

                }



                if (string.IsNullOrWhiteSpace(json)) return false;



                var cloudItems = JsonSerializer.Deserialize<List<SlideTemplateItem>>(json);

                if (cloudItems == null) return true;



                foreach (var ci in cloudItems)

                {

                    if (ci == null || string.IsNullOrEmpty(ci.id) || string.IsNullOrEmpty(ci.pptxUrl)) continue;

                    ci.localPptxPath = ""; // Tuyệt đối không giữ đường dẫn cục bộ

                    ci.isCustom = false;

                    var existing = targetList.Find(t => IsSameTemplate(t, ci.title, ci.id, ci.pptxUrl, null));

                    if (existing == null)

                    {

                        targetList.Add(ci);

                    }

                }

                return true;

            }

            catch

            {

                return false;

            }

        }



        private static async Task<bool> ScanGitHubPptxFilesAsync(string apiEndpoint, List<SlideTemplateItem> targetList)

        {

            try

            {

                using var req = new HttpRequestMessage(HttpMethod.Get, apiEndpoint);

                req.Headers.UserAgent.ParseAdd("ZenQOffice/1.0");

                string token = GetGitHubTokenFromCredentialManager();

                if (!string.IsNullOrWhiteSpace(token))

                {

                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);

                }



                var resp = await _httpClient.SendAsync(req).ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode) return false;



                string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (string.IsNullOrEmpty(json)) return true;



                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind != JsonValueKind.Array) return true;



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

                                string baseFileName = Path.GetFileNameWithoutExtension(name);

                                string displayTitle = baseFileName.Replace("_", " ");

                                string tmplId = TemplateUploadDialogForm.GenerateSlugId(displayTitle);



                                bool alreadyExists = targetList.Exists(t =>

                                    IsSameTemplate(t, displayTitle, tmplId, downloadUrl, null) ||

                                    IsSameTemplate(t, baseFileName, baseFileName, downloadUrl, null));



                                if (!alreadyExists)

                                {

                                    var cloudTmpl = new SlideTemplateItem

                                    {

                                        id = tmplId,

                                        title = displayTitle,

                                        subtitle = "",

                                        category = "Thuyết trình",

                                        tags = new List<string> { displayTitle, "Thuyết trình" },

                                        slideCount = 1,

                                        accentColor = "#3B82F6",

                                        localPptxPath = "",

                                        pptxUrl = downloadUrl,

                                        isCustom = false

                                    };

                                    targetList.Add(cloudTmpl);

                                }

                            }

                        }

                    }

                }

                return true;

            }

            catch

            {

                return false;

            }

        }



        public static Bitmap GetSlideBitmap(string templateId, int slideIndex)

        {

            if (!CheckInternetConnected())

            {

                ClearMemoryForOffline();

                return null;

            }



            string cacheKey = $"{templateId}_{slideIndex}";

            lock (_syncLock)

            {

                if (_bitmapCache.TryGetValue(cacheKey, out Bitmap bmp) && bmp != null)

                {

                    // Đưa lên đầu danh sách vừa sử dụng (LRU touch)

                    _lruKeys.Remove(cacheKey);

                    _lruKeys.AddLast(cacheKey);

                    return bmp;

                }

            }



            QueueSlideRender(templateId, slideIndex);

            return null;

        }



        public static void QueueSlideRender(string templateId, int slideIndex)

        {

            if (IsOffline || !CheckInternetConnected()) return;

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



                if (!CheckInternetConnected())

                {

                    ClearMemoryForOffline();

                    try { OnSlideRendered?.Invoke(); } catch { }

                    return;

                }



                try

                {

                    await Task.Delay(12).ConfigureAwait(false);



                    string key = $"{item.templateId}_{item.slideIndex}";

                    lock (_syncLock)

                    {

                        if (_bitmapCache.ContainsKey(key)) continue;

                    }



                    SlideTemplateItem tmpl = null;

                    lock (_syncLock)

                    {

                        tmpl = Templates.Find(t => t.id == item.templateId);

                    }

                    if (tmpl == null) continue;



                    // 1. Thử tải trực tiếp ảnh thu nhỏ (thumbnail .jpg) đã dựng sẵn trên GitHub vào RAM (không cần tải file .pptx)

                    string thumbUrl = $"{GITHUB_RAW_BASE}/thumbs/{Uri.EscapeDataString(tmpl.id)}_{item.slideIndex}.jpg";

                    try

                    {

                        using var req = new HttpRequestMessage(HttpMethod.Get, thumbUrl);

                        req.Headers.UserAgent.ParseAdd("ZenQOffice/1.0");

                        using var resp = await _httpClient.SendAsync(req).ConfigureAwait(false);

                        if (resp.IsSuccessStatusCode)

                        {

                            byte[] imgBytes = await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                            if (imgBytes != null && imgBytes.Length > 0)

                            {

                                using var ms = new MemoryStream(imgBytes);

                                using var rawImg = Image.FromStream(ms);

                                int targetW = Math.Min(rawImg.Width, 640);

                                int targetH = (rawImg.Width > 640) ? Math.Max(1, (int)Math.Round(rawImg.Height * (640.0 / rawImg.Width))) : rawImg.Height;

                                Bitmap ramBmp = new Bitmap(targetW, targetH, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                                using (var tg = Graphics.FromImage(ramBmp))

                                {

                                    tg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                                    tg.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                                    tg.DrawImage(rawImg, 0, 0, targetW, targetH);

                                }

                                PutBitmapToCache(key, ramBmp);

                                try { OnSlideRendered?.Invoke(); } catch { }

                                continue;

                            }

                        }

                    }

                    catch { }



                    // 2. Nếu trên GitHub chưa có sẵn ảnh thumb .jpg của mẫu này:

                    // Tải tạm .pptx vào thư mục làm việc, kết xuất toàn bộ các slide vào RAM, đẩy ảnh thumb .jpg lên GitHub để lần sau chỉ tải ảnh,

                    // và XÓA NGAY LẬP TỨC tệp .pptx tạm cùng tài nguyên giải nén trong khối finally!

                    if (!string.IsNullOrEmpty(tmpl.pptxUrl))

                    {

                        string tempPptx = Path.Combine(TempCleanupManager.ActiveTabWorkDirectory, $"preview_tmp_{Guid.NewGuid():N}.pptx");

                         PresentationRoot previewPres = null;

                        var generatedThumbs = new List<(int Index, byte[] JpgBytes)>();

                        try

                        {

                            byte[] pptxBytes = await _httpClient.GetByteArrayAsync(tmpl.pptxUrl).ConfigureAwait(false);

                            if (pptxBytes != null && pptxBytes.Length > 0)

                            {

                                await File.WriteAllBytesAsync(tempPptx, pptxBytes).ConfigureAwait(false);

                                lock (_renderLock)

                                {

                                    previewPres = ZenQOffice.Format.PptxHandler.LoadPptx(tempPptx);

                                    var pres = previewPres;

                                    if (pres != null && pres.Slides != null && pres.Slides.Count > 0)

                                    {

                                        tmpl.slideCount = pres.Slides.Count;

                                        var sv = new ZenQOffice.UI.SlidesView { Presentation = pres };

                                        for (int sIdx = 0; sIdx < pres.Slides.Count; sIdx++)

                                        {

                                            sv.ActiveSlideIndex = sIdx;

                                            float tmAr = (pres.CanvasHeight > 0) ? (pres.CanvasWidth / pres.CanvasHeight) : (16f / 9f);
                                            int thumbTargetW = 960;
                                            int thumbTargetH = (int)Math.Round(960f / tmAr);
                                            if (thumbTargetH > 960)
                                            {
                                                thumbTargetH = 960;
                                                thumbTargetW = (int)Math.Round(960f * tmAr);
                                            }
                                            Bitmap slideBmp = sv.RenderSlideToBitmap(pres.Slides[sIdx], thumbTargetW, thumbTargetH);

                                            if (slideBmp != null)

                                            {

                                                string sKey = $"{tmpl.id}_{sIdx}";

                                                PutBitmapToCache(sKey, slideBmp);

                                                try

                                                {

                                                    using var msJpg = new MemoryStream();

                                                    slideBmp.Save(msJpg, ImageFormat.Jpeg);

                                                    generatedThumbs.Add((sIdx, msJpg.ToArray()));

                                                }

                                                catch { }

                                            }

                                        }

                                        // Giải phóng ngay các Bitmap trong Presentation tạm để không giữ khóa file

                                        foreach (var sl in pres.Slides)

                                        {

                                            if (sl?.Shapes == null) continue;

                                            foreach (var sh in sl.Shapes)

                                            {

                                                try { sh.CachedImage?.Dispose(); sh.CachedImage = null; } catch { }

                                            }

                                        }

                                    }

                                }

                            }

                        }

                        catch (Exception ex)

                        {

                            // Nếu tệp pptx bị 404 hoặc không tải được vì đã bị xóa trên GitHub:

                            bool is404 = (ex is HttpRequestException hex && hex.StatusCode == System.Net.HttpStatusCode.NotFound) ||

                                         (ex.Message != null && ex.Message.Contains("404"));

                            if (is404)

                            {

                                lock (_syncLock)

                                {

                                    Templates.RemoveAll(t => IsSameTemplate(t, tmpl.title, tmpl.id, tmpl.pptxUrl, null));

                                }

                                try { OnSlideRendered?.Invoke(); } catch { }

                            }

                        }

                        finally

                        {

                            TempCleanupManager.DisposeAndDeletePresentationTempMedia(previewPres, tempPptx);

                        }



                        try { OnSlideRendered?.Invoke(); } catch { }



                        if (generatedThumbs.Count > 0)

                        {

                            _ = Task.Run(async () =>

                            {

                                try

                                {

                                    await UploadSlideThumbsToGitHubAsync(tmpl.id, generatedThumbs).ConfigureAwait(false);

                                }

                                catch { }

                            });

                        }

                    }

                }

                catch

                {

                }

            }

        }



        /// <summary>

        /// CHỈ KHI NGƯỜI DÙNG BẤM VÀO MẪU ĐỂ SỬ DỤNG:

        /// Tải tệp .pptx của mẫu đó từ GitHub Cloud về thư mục làm việc tạm của tab (TempCleanupManager.ActiveTabWorkDirectory).

        /// Tuyệt đối không lưu cố định ở AppData; mẫu đã tải có thể tải lại bất cứ lúc nào; khi đóng tab sẽ tự động xóa sạch.

        /// </summary>

        public static string DownloadTemplateForTabUse(SlideTemplateItem tmpl, out string errorMessage)

        {

            errorMessage = null;

            if (tmpl == null)

            {

                errorMessage = AppL10n.T("Mẫu không hợp lệ.", "Invalid template.");

                return null;

            }



            if (!CheckInternetConnected())

            {

                ClearMemoryForOffline();

                try { OnSlideRendered?.Invoke(); } catch { }

                errorMessage = AppL10n.T("Mất kết nối Internet. Mẫu thuyết trình Cloud không thể sử dụng khi ngoại tuyến.", "No Internet connection. Cloud templates are unavailable offline.");

                return null;

            }



            if (string.IsNullOrEmpty(tmpl.pptxUrl))

            {

                errorMessage = AppL10n.T("Mẫu này chưa có liên kết đám mây hợp lệ.", "This template does not have a valid cloud link.");

                return null;

            }



            try

            {

                string workDir = TempCleanupManager.ActiveTabWorkDirectory;

                string safeName = CleanFileName(tmpl.title);

                string tabTempPptxPath = Path.Combine(workDir, $"tpl_tab_{Guid.NewGuid():N}_{safeName}.pptx");



                // Cho phép thời gian tải tối đa 3 phút cho các tệp mẫu dung lượng lớn (30MB - 100MB)

                using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(3) })

                {

                    client.DefaultRequestHeaders.UserAgent.ParseAdd("ZenQOffice/1.0");

                    using var resp = client.GetAsync(tmpl.pptxUrl, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();

                    if (resp.IsSuccessStatusCode)

                    {

                        using var stream = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

                        using var fs = new FileStream(tabTempPptxPath, FileMode.Create, FileAccess.Write, FileShare.None, 131072);

                        stream.CopyTo(fs);

                        fs.Flush();



                        if (new FileInfo(tabTempPptxPath).Length > 0)

                        {

                            RecordTemplateUsage(tmpl.id);

                            return tabTempPptxPath;

                        }

                    }

                    else if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)

                    {

                        errorMessage = AppL10n.T("Tệp mẫu chưa hoàn tất xử lý trên máy chủ đám mây (404 Not Found). Vui lòng thử lại sau giây lát.", "Template file not yet available on cloud server (404 Not Found). Please try again shortly.");

                        return null; // Không xóa bộ nhớ hay ngắt mạng khi lỗi do tệp chưa sẵn sàng

                    }

                    else

                    {

                        errorMessage = string.Format(AppL10n.T("Máy chủ phản hồi mã lỗi {0} ({1}). Vui lòng thử lại.", "Server returned error {0} ({1}). Please try again."), (int)resp.StatusCode, resp.ReasonPhrase);

                        return null;

                    }

                }



                errorMessage = AppL10n.T("Không thể tải dữ liệu mẫu từ đám mây.", "Unable to download template data from the cloud.");

                return null;

            }

            catch (Exception ex)

            {

                // Chỉ xóa bộ nhớ và báo mất mạng khi THỰC SỰ mất kết nối Internet cục bộ

                if (!CheckInternetConnected())

                {

                    ClearMemoryForOffline();

                    try { OnSlideRendered?.Invoke(); } catch { }

                    errorMessage = AppL10n.T("Mất kết nối Internet. Vui lòng kiểm tra đường truyền mạng và thử lại.", "No Internet connection. Please check your network and try again.");

                }

                else

                {

                    errorMessage = string.Format(AppL10n.T("Tải mẫu '{0}' không thành công (tệp lớn hoặc đường truyền chậm): {1}", "Failed to download template '{0}' (large file or slow network): {1}"), tmpl.title, ex.Message);

                }

                return null;

            }

        }



        public static string TryRecoverTemplateForTabByTitle(string tabTitle)

        {

            if (string.IsNullOrWhiteSpace(tabTitle) || !CheckInternetConnected()) return null;

            try

            {

                string cleanTitle = tabTitle.Trim();

                if (cleanTitle.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase))

                    cleanTitle = cleanTitle.Substring(0, cleanTitle.Length - 5).Trim();

                else if (cleanTitle.EndsWith(".zenq", StringComparison.OrdinalIgnoreCase))

                    cleanTitle = cleanTitle.Substring(0, cleanTitle.Length - 5).Trim();



                if (string.IsNullOrEmpty(cleanTitle) || cleanTitle.StartsWith("Trình bày", StringComparison.OrdinalIgnoreCase) || cleanTitle.StartsWith("Bài thuyết trình", StringComparison.OrdinalIgnoreCase))

                    return null;



                string slug = TemplateUploadDialogForm.GenerateSlugId(cleanTitle);

                SlideTemplateItem matched = null;

                lock (_syncLock)

                {

                    matched = Templates.Find(t => IsSameTemplate(t, cleanTitle, slug, null, null));

                }



                if (matched == null)

                {

                    var tempList = new List<SlideTemplateItem>();

                    LoadCloudCustomCatalogManifestAsync(tempList).GetAwaiter().GetResult();

                    matched = tempList.Find(t => IsSameTemplate(t, cleanTitle, slug, null, null));

                }



                if (matched != null)

                {

                    return DownloadTemplateForTabUse(matched, out _);

                }

            }

            catch { }

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



        /// <summary>

        /// Tải mẫu mới từ máy người dùng trực tiếp lên kho đám mây GitHub (danhtruong6112009-source/zenq-slide-templates).

        /// Tuyệt đối KHÔNG sao chép hay lưu tệp vào AppData.

        /// </summary>

        public static SlideTemplateItem AddCustomTemplateFromPptx(

            string pptxPath,

            string customTitle = null,

            string customId = null,

            string customCategory = null,

            List<string> customTags = null,

            bool syncToCloud = true,

            bool waitForUpload = false)

        {

            if (string.IsNullOrEmpty(pptxPath) || !File.Exists(pptxPath)) return null;

            if (DatabaseAuthService.CurrentUser != null && DatabaseAuthService.CurrentUser.Ban)
            {
                return null;
            }

            if (!CheckInternetConnected())

            {

                ClearMemoryForOffline();

                return null;

            }



            string name = !string.IsNullOrWhiteSpace(customTitle)

                ? customTitle.Trim()

                : Path.GetFileNameWithoutExtension(pptxPath);



            string id = !string.IsNullOrWhiteSpace(customId)

                ? customId.Trim()

                : TemplateUploadDialogForm.GenerateSlugId(name);



            int slideCount = 1;

            var generatedThumbs = new List<(int Index, byte[] JpgBytes)>();

            PresentationRoot uploadPres = null;

            try

            {

                uploadPres = ZenQOffice.Format.PptxHandler.LoadPptx(pptxPath);

                var pres = uploadPres;

                if (pres != null && pres.Slides != null && pres.Slides.Count > 0)

                {

                    slideCount = pres.Slides.Count;

                    var sv = new ZenQOffice.UI.SlidesView { Presentation = pres };

                    for (int sIdx = 0; sIdx < pres.Slides.Count; sIdx++)

                    {

                        sv.ActiveSlideIndex = sIdx;

                        float tmAr = (pres.CanvasHeight > 0) ? (pres.CanvasWidth / pres.CanvasHeight) : (16f / 9f);
                        int thumbTargetW = 960;
                        int thumbTargetH = (int)Math.Round(960f / tmAr);
                        if (thumbTargetH > 960)
                        {
                            thumbTargetH = 960;
                            thumbTargetW = (int)Math.Round(960f * tmAr);
                        }
                        Bitmap bmp = sv.RenderSlideToBitmap(pres.Slides[sIdx], thumbTargetW, thumbTargetH);

                        if (bmp != null)

                        {

                            PutBitmapToCache($"{id}_{sIdx}", bmp);

                            try

                            {

                                using var ms = new MemoryStream();

                                bmp.Save(ms, ImageFormat.Jpeg);

                                generatedThumbs.Add((sIdx, ms.ToArray()));

                            }

                            catch { }

                        }

                    }

                    foreach (var sl in pres.Slides)

                    {

                        if (sl?.Shapes == null) continue;

                        foreach (var sh in sl.Shapes)

                        {

                            try { sh.CachedImage?.Dispose(); sh.CachedImage = null; } catch { }

                        }

                    }

                }

            }

            catch { }

            finally

            {

                TempCleanupManager.DisposeAndDeletePresentationTempMedia(uploadPres, null);

            }



            List<string> finalTags = new List<string>();

            if (customTags != null && customTags.Count > 0)

            {

                foreach (var t in customTags)

                {

                    if (!string.IsNullOrWhiteSpace(t) && !finalTags.Contains(t.Trim()))

                        finalTags.Add(t.Trim());

                }

            }

            if (finalTags.Count == 0)

            {

                finalTags.Add(name);

            }



            string safeFileName = CleanFileName(name) + ".pptx";

            string rawDownloadUrl = $"{GITHUB_RAW_BASE}/{Uri.EscapeDataString(safeFileName)}";



            SlideTemplateItem cloudItem = new SlideTemplateItem

            {

                id = id,

                title = name,

                subtitle = string.Join(", ", finalTags),

                category = !string.IsNullOrWhiteSpace(customCategory) ? customCategory.Trim() : "Thuyết trình",

                tags = finalTags,

                slideCount = slideCount,

                accentColor = "#6366F1",

                isCustom = false,

                pptxUrl = rawDownloadUrl,

                localPptxPath = "" // Không lưu bất kỳ file mẫu nào vào AppData

            };



            lock (_syncLock)

            {

                IsOffline = false;

                Templates.RemoveAll(t => IsSameTemplate(t, cloudItem.title, cloudItem.id, cloudItem.pptxUrl, null));

                Templates.Insert(0, cloudItem);

            }



            try { OnSlideRendered?.Invoke(); } catch { }



            string sourceFileToUpload = pptxPath;

            if (waitForUpload)

            {

                try

                {

                    bool ok = UploadTemplateToGitHubCloudAsync(cloudItem, sourceFileToUpload, generatedThumbs).GetAwaiter().GetResult();

                    if (!ok) return null;

                }

                catch

                {

                    return null;

                }

            }

            else

            {

                Task.Run(async () =>

                {

                    try

                    {

                        await UploadTemplateToGitHubCloudAsync(cloudItem, sourceFileToUpload, generatedThumbs).ConfigureAwait(false);

                    }

                    catch { }

                });

            }



            return cloudItem;

        }



        #region GitHub Cloud Repository Upload (danhtruong6112009-source/zenq-slide-templates)



        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]

        private struct CREDENTIAL

        {

            public uint Flags;

            public uint Type;

            public IntPtr TargetName;

            public IntPtr Comment;

            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;

            public uint CredentialBlobSize;

            public IntPtr CredentialBlob;

            public uint Persist;

            public uint AttributeCount;

            public IntPtr Attributes;

            public IntPtr TargetAlias;

            public IntPtr UserName;

        }



        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]

        private static extern bool CredRead(string target, uint type, int reservedFlag, out IntPtr credentialPtr);



        [DllImport("advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]

        private static extern void CredFree(IntPtr cred);



        private static string GetGitHubTokenFromCredentialManager()

        {

            string envToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

            if (!string.IsNullOrWhiteSpace(envToken)) return envToken.Trim();



            string[] targets = new[]

            {

                "git:https://danhtruong6112009-source@github.com",

                "git:https://github.com"

            };



            foreach (string target in targets)

            {

                IntPtr credPtr = IntPtr.Zero;

                try

                {

                    if (CredRead(target, 1, 0, out credPtr) && credPtr != IntPtr.Zero)

                    {

                        CREDENTIAL cred = Marshal.PtrToStructure<CREDENTIAL>(credPtr);

                        if (cred.CredentialBlob != IntPtr.Zero && cred.CredentialBlobSize > 0)

                        {

                            string blob = Marshal.PtrToStringUni(cred.CredentialBlob, (int)cred.CredentialBlobSize / 2);

                            if (!string.IsNullOrWhiteSpace(blob))

                            {

                                return blob.Trim('\0', ' ', '\r', '\n');

                            }

                        }

                    }

                }

                catch { }

                finally

                {

                    if (credPtr != IntPtr.Zero) CredFree(credPtr);

                }

            }

            return null;

        }



        public static async Task<bool> UploadTemplateToGitHubCloudAsync(

            SlideTemplateItem tmpl,

            string sourcePptxFile,

            List<(int Index, byte[] JpgBytes)> thumbs = null)

        {

            if (tmpl == null || string.IsNullOrEmpty(sourcePptxFile) || !File.Exists(sourcePptxFile))

                return false;



            string token = GetGitHubTokenFromCredentialManager();

            if (string.IsNullOrWhiteSpace(token)) return false;



            try

            {

                string safeFileName = CleanFileName(tmpl.title) + ".pptx";

                string apiUrl = $"https://api.github.com/repos/danhtruong6112009-source/zenq-slide-templates/contents/templates/{Uri.EscapeDataString(safeFileName)}";



                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };

                client.DefaultRequestHeaders.UserAgent.ParseAdd("ZenQOffice-Uploader/1.0");

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);

                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");



                string existingSha = await GetGitHubFileShaAsync(client, apiUrl).ConfigureAwait(false);



                byte[] fileBytes = await File.ReadAllBytesAsync(sourcePptxFile).ConfigureAwait(false);

                var payloadObj = new Dictionary<string, object>

                {

                    ["message"] = $"Upload template: {tmpl.title} ({tmpl.id})",

                    ["content"] = Convert.ToBase64String(fileBytes),

                    ["branch"] = "main"

                };

                if (!string.IsNullOrEmpty(existingSha))

                {

                    payloadObj["sha"] = existingSha;

                }



                using var putContent = new StringContent(JsonSerializer.Serialize(payloadObj), Encoding.UTF8, "application/json");

                var putResp = await client.PutAsync(apiUrl, putContent).ConfigureAwait(false);

                if (!putResp.IsSuccessStatusCode) return false;



                tmpl.pptxUrl = $"{GITHUB_RAW_BASE}/{Uri.EscapeDataString(safeFileName)}";

                tmpl.localPptxPath = "";



                if (thumbs != null && thumbs.Count > 0)

                {

                    await UploadSlideThumbsToGitHubAsync(tmpl.id, thumbs, client).ConfigureAwait(false);

                }



                await UpdateCloudCustomCatalogJsonAsync(client, tmpl).ConfigureAwait(false);

                return true;

            }

            catch

            {

                return false;

            }

        }



        private static async Task UploadSlideThumbsToGitHubAsync(string templateId, List<(int Index, byte[] JpgBytes)> thumbs, HttpClient existingClient = null)

        {

            if (string.IsNullOrEmpty(templateId) || thumbs == null || thumbs.Count == 0) return;

            string token = GetGitHubTokenFromCredentialManager();

            if (string.IsNullOrWhiteSpace(token)) return;



            HttpClient client = existingClient;

            bool disposeClient = false;

            if (client == null)

            {

                client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

                client.DefaultRequestHeaders.UserAgent.ParseAdd("ZenQOffice-Uploader/1.0");

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);

                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");

                disposeClient = true;

            }



            try

            {

                foreach (var th in thumbs)

                {

                    if (th.JpgBytes == null || th.JpgBytes.Length == 0) continue;

                    string apiUrl = $"https://api.github.com/repos/danhtruong6112009-source/zenq-slide-templates/contents/templates/thumbs/{Uri.EscapeDataString(templateId)}_{th.Index}.jpg";

                    string sha = await GetGitHubFileShaAsync(client, apiUrl).ConfigureAwait(false);



                    var putObj = new Dictionary<string, object>

                    {

                        ["message"] = $"Upload slide preview thumb {templateId}_{th.Index}.jpg",

                        ["content"] = Convert.ToBase64String(th.JpgBytes),

                        ["branch"] = "main"

                    };

                    if (!string.IsNullOrEmpty(sha)) putObj["sha"] = sha;



                    using var content = new StringContent(JsonSerializer.Serialize(putObj), Encoding.UTF8, "application/json");

                    await client.PutAsync(apiUrl, content).ConfigureAwait(false);

                }

            }

            catch { }

            finally

            {

                if (disposeClient) client.Dispose();

            }

        }



        private static async Task<string> GetGitHubFileShaAsync(HttpClient client, string apiUrl)

        {

            try

            {

                var getResp = await client.GetAsync(apiUrl).ConfigureAwait(false);

                if (getResp.IsSuccessStatusCode)

                {

                    string getJson = await getResp.Content.ReadAsStringAsync().ConfigureAwait(false);

                    using var doc = JsonDocument.Parse(getJson);

                    if (doc.RootElement.TryGetProperty("sha", out var shaProp))

                    {

                        return shaProp.GetString();

                    }

                }

            }

            catch { }

            return null;

        }



        private static async Task UpdateCloudCustomCatalogJsonAsync(HttpClient client, SlideTemplateItem uploadedItem)

        {

            try

            {

                string existingSha = null;

                List<SlideTemplateItem> catalog = new List<SlideTemplateItem>();



                var getResp = await client.GetAsync(GITHUB_CATALOG_MANIFEST_API).ConfigureAwait(false);

                if (getResp.IsSuccessStatusCode)

                {

                    string getJson = await getResp.Content.ReadAsStringAsync().ConfigureAwait(false);

                    using var doc = JsonDocument.Parse(getJson);

                    if (doc.RootElement.TryGetProperty("sha", out var shaProp))

                    {

                        existingSha = shaProp.GetString();

                    }

                    if (doc.RootElement.TryGetProperty("content", out var contentProp))

                    {

                        string b64 = (contentProp.GetString() ?? "").Replace("\n", "").Replace("\r", "");

                        if (!string.IsNullOrEmpty(b64))

                        {

                            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(b64));

                            var parsed = JsonSerializer.Deserialize<List<SlideTemplateItem>>(decoded);

                            if (parsed != null) catalog = parsed;

                        }

                    }

                }



                catalog.RemoveAll(x => x != null && IsSameTemplate(x, uploadedItem.title, uploadedItem.id, uploadedItem.pptxUrl, null));



                catalog.Insert(0, new SlideTemplateItem

                {

                    id = uploadedItem.id,

                    title = uploadedItem.title,

                    subtitle = uploadedItem.subtitle,

                    category = uploadedItem.category,

                    tags = uploadedItem.tags ?? new List<string>(),

                    slideCount = uploadedItem.slideCount,

                    accentColor = uploadedItem.accentColor,

                    isCustom = false,

                    pptxUrl = uploadedItem.pptxUrl,

                    localPptxPath = ""

                });



                string newCatalogJson = JsonSerializer.Serialize(catalog, new JsonSerializerOptions { WriteIndented = true });

                var putObj = new Dictionary<string, object>

                {

                    ["message"] = $"Update custom_catalog.json for {uploadedItem.title}",

                    ["content"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(newCatalogJson)),

                    ["branch"] = "main"

                };

                if (!string.IsNullOrEmpty(existingSha))

                {

                    putObj["sha"] = existingSha;

                }



                using var putContent = new StringContent(JsonSerializer.Serialize(putObj), Encoding.UTF8, "application/json");

                await client.PutAsync(GITHUB_CATALOG_MANIFEST_API, putContent).ConfigureAwait(false);

            }

            catch { }

        }



        #endregion



        /// <summary>

        /// Giải phóng bộ nhớ ảnh thu nhỏ trên RAM và xóa sạch mọi dấu vết thư mục cache cũ.

        /// </summary>

        
        public static void EvictTemplateBitmaps(string templateId)
        {
            if (string.IsNullOrEmpty(templateId)) return;
            lock (_syncLock)
            {
                string prefix = templateId + "_";
                var keysToRemove = _bitmapCache.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var k in keysToRemove)
                {
                    if (_bitmapCache.Remove(k, out var bmp))
                    {
                        try { bmp?.Dispose(); } catch { }
                    }
                    _lruKeys.Remove(k);
                    _queuedOrDone.Remove(k);
                }
            }
        }

        public static async Task<(bool Success, string Error)> DeleteTemplateFromCloudAsync(SlideTemplateItem tmpl)
        {
            if (tmpl == null) return (false, AppL10n.T("Mẫu không tồn tại.", "Template does not exist."));

            string token = GetGitHubTokenFromCredentialManager();
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, AppL10n.T("Không tìm thấy quyền chứng thực quản trị viên (GitHub Token).", "Missing administrator authorization (GitHub Token)."));
            }

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ZenQOffice-Admin/1.0");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");

                // 1. Xác định tên tệp .pptx
                string fileName = "";
                if (!string.IsNullOrEmpty(tmpl.pptxUrl))
                {
                    try
                    {
                        var uri = new Uri(tmpl.pptxUrl);
                        fileName = Path.GetFileName(uri.LocalPath);
                    }
                    catch { }
                }
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = CleanFileName(tmpl.title) + ".pptx";
                }

                // Xóa tệp .pptx trên GitHub
                string fileApiUrl = $"https://api.github.com/repos/danhtruong6112009-source/zenq-slide-templates/contents/templates/{Uri.EscapeDataString(fileName)}";
                await DeleteGitHubFileIfExistsAsync(client, fileApiUrl, $"Delete template file {fileName}").ConfigureAwait(false);

                // 2. Xóa các tệp ảnh thu nhỏ (thumbs)
                int maxSlides = Math.Max(25, tmpl.slideCount + 3);
                for (int s = 0; s < maxSlides; s++)
                {
                    string thumbApiUrl = $"https://api.github.com/repos/danhtruong6112009-source/zenq-slide-templates/contents/templates/thumbs/{Uri.EscapeDataString(tmpl.id)}_{s}.jpg";
                    bool deleted = await DeleteGitHubFileIfExistsAsync(client, thumbApiUrl, $"Delete thumb {tmpl.id}_{s}.jpg").ConfigureAwait(false);
                    if (!deleted && s >= tmpl.slideCount)
                    {
                        break;
                    }
                }

                // 3. Xóa khỏi custom_catalog.json trên GitHub
                await RemoveFromCloudCustomCatalogJsonAsync(client, tmpl).ConfigureAwait(false);

                // 4. Xóa khỏi bộ nhớ RAM cục bộ
                lock (_syncLock)
                {
                    Templates.RemoveAll(t => IsSameTemplate(t, tmpl.title, tmpl.id, tmpl.pptxUrl, null));
                    EvictTemplateBitmaps(tmpl.id);
                }

                try { OnSlideRendered?.Invoke(); } catch { }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, AppL10n.T("Lỗi khi xóa mẫu: ", "Error deleting template: ") + ex.Message);
            }
        }

        private static async Task<bool> DeleteGitHubFileIfExistsAsync(HttpClient client, string apiUrl, string commitMessage)
        {
            try
            {
                string sha = await GetGitHubFileShaAsync(client, apiUrl).ConfigureAwait(false);
                if (string.IsNullOrEmpty(sha)) return false;

                var deletePayload = new Dictionary<string, object>
                {
                    ["message"] = commitMessage,
                    ["sha"] = sha,
                    ["branch"] = "main"
                };

                using var request = new HttpRequestMessage(HttpMethod.Delete, apiUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(deletePayload), Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(request).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static async Task RemoveFromCloudCustomCatalogJsonAsync(HttpClient client, SlideTemplateItem tmpl)
        {
            try
            {
                string existingSha = null;
                List<SlideTemplateItem> catalog = new List<SlideTemplateItem>();

                var getResp = await client.GetAsync(GITHUB_CATALOG_MANIFEST_API).ConfigureAwait(false);
                if (getResp.IsSuccessStatusCode)
                {
                    string getJson = await getResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using var doc = JsonDocument.Parse(getJson);
                    if (doc.RootElement.TryGetProperty("sha", out var shaProp))
                    {
                        existingSha = shaProp.GetString();
                    }
                    if (doc.RootElement.TryGetProperty("content", out var contentProp))
                    {
                        string b64 = (contentProp.GetString() ?? "").Replace("\n", "").Replace("\r", "");
                        if (!string.IsNullOrEmpty(b64))
                        {
                            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                            var parsed = JsonSerializer.Deserialize<List<SlideTemplateItem>>(decoded);
                            if (parsed != null) catalog = parsed;
                        }
                    }
                }

                int removed = catalog.RemoveAll(x => x != null && IsSameTemplate(x, tmpl.title, tmpl.id, tmpl.pptxUrl, null));
                if (removed > 0 && !string.IsNullOrEmpty(existingSha))
                {
                    string newCatalogJson = JsonSerializer.Serialize(catalog, new JsonSerializerOptions { WriteIndented = true });
                    var putObj = new Dictionary<string, object>
                    {
                        ["message"] = $"Remove {tmpl.title} from custom_catalog.json",
                        ["content"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(newCatalogJson)),
                        ["branch"] = "main",
                        ["sha"] = existingSha
                    };

                    using var putContent = new StringContent(JsonSerializer.Serialize(putObj), Encoding.UTF8, "application/json");
                    await client.PutAsync(GITHUB_CATALOG_MANIFEST_API, putContent).ConfigureAwait(false);
                }
            }
            catch { }
        }

        public static void CleanupCache()

        {

            lock (_syncLock)

            {

                foreach (var bmp in _bitmapCache.Values)

                {

                    try { bmp?.Dispose(); } catch { }

                }

                _bitmapCache.Clear();

                _lruKeys.Clear();

                _queuedOrDone.Clear();

                _renderQueue.Clear();

            }

            TempCleanupManager.PurgeAllCloudCachesOnDisk();

        }

    }

}


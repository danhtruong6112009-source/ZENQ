using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZenQOffice.Core
{
    public class OnlineGraphicsCategory
    {
        public string name { get; set; }
        public List<OnlineGraphicItem> items { get; set; }
    }

    public class OnlineGraphicItem
    {
        public string id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string type { get; set; }
    }

    public class OnlineGraphicsResponse
    {
        public string version { get; set; }
        public List<OnlineGraphicsCategory> categories { get; set; }
    }

    /// <summary>
    /// Quản lý Kho Đồ họa Vector 100% Cloud-Only:
    /// - Tuyệt đối KHÔNG tạo hay lưu thư mục Cache\Graphics trong AppData, không dùng dữ liệu offline trên ổ cứng.
    /// - Khi duyệt danh sách: chỉ tải dữ liệu vào RAM để hiển thị thumbnail.
    /// - Khi mất kết nối mạng: báo trạng thái IsOffline = true ("Mất kết nối Internet").
    /// - Chỉ khi người dùng bấm chọn sử dụng một đồ họa vào tab thì mới tải tệp đó về thư mục làm việc tạm của tab
    ///   (TempCleanupManager.ActiveTabWorkDirectory), và khi đóng tab sẽ tự động xóa sạch ngay lập tức.
    /// </summary>
    public static class OnlineGraphicsManager
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        private static OnlineGraphicsResponse _catalog;
        private static readonly Dictionary<string, byte[]> _ramDataCache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _downloadingImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _cacheLock = new object();

        public static bool IsOffline { get; private set; } = false;
        public static bool IsLoading { get; private set; } = false;

        public static event Action<OnlineGraphicsResponse> OnCatalogUpdated;
        public static event Action OnImageDownloaded;

        private static System.Threading.Timer _throttleTimer;
        private static readonly object _throttleLock = new object();

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

        public static void TriggerImageDownloaded()
        {
            lock (_throttleLock)
            {
                if (_throttleTimer == null)
                {
                    _throttleTimer = new System.Threading.Timer(_ =>
                    {
                        lock (_throttleLock)
                        {
                            try { _throttleTimer?.Dispose(); } catch { }
                            _throttleTimer = null;
                        }
                        try { OnImageDownloaded?.Invoke(); } catch { }
                    }, null, 150, System.Threading.Timeout.Infinite);
                }
            }
        }

        private static OnlineGraphicsResponse NormalizeCatalogCategories(OnlineGraphicsResponse cat)
        {
            if (cat == null || cat.categories == null) return cat;
            foreach (var c in cat.categories)
            {
                if (c == null || string.IsNullOrEmpty(c.name)) continue;
                string n = c.name.Trim();
                if (n.IndexOf("phức tạp", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Scenes", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.name = "Hoạt cảnh";
                }
                else if (n.IndexOf("unDraw", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.name = "Chủ đề sự vật";
                }
                else if (n.IndexOf("Color Emojis", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.name = "Biểu tượng";
                }
                else if (n.IndexOf("Flags", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.name = "Quốc kỳ";
                }
                else if (n.IndexOf("Băng rôn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("lễ hội", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("festive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         (c.items != null && c.items.Exists(it => it.id != null && it.id.Contains("festive_banners"))))
                {
                    c.name = "Băng rôn";
                }
                else if (n.IndexOf("Lụa", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Vải mềm", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.name = "Lụa mềm";
                }
                else if (n.IndexOf("Bụi cỏ", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Cây cỏ thiên nhiên", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.name = "Bụi cỏ";
                }
                else if (n.IndexOf("Vàng Gold", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("hoàng kim cao cấp", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.name = "Vàng Gold";
                }
                else if (n.IndexOf("Cành lá hoàng kim", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Cây lá dát vàng", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.name = "Cây lá vàng";
                }
                else if (n.IndexOf("Ấn Độ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         (c.items != null && c.items.Exists(it => it.id != null && it.id.Contains("heritage_gold"))))
                {
                    c.name = "Di sản Việt Nam";
                }
                else if (n.IndexOf("Tranh nhân vật", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         (c.items != null && c.items.Exists(it => it.id != null && it.id.Contains("people_vn"))))
                {
                    c.name = "Đời sống Việt Nam";
                }
                else if (n.IndexOf("Hào khí", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Việt Nam", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.name = "Việt Nam";
                }
                else if (n.IndexOf("Gradient", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.name = "Gradient";
                }

                if (c.name.Contains("&"))
                {
                    c.name = c.name.Replace("&", "").Replace("  ", " ").Trim();
                }
            }

            var merged = new List<OnlineGraphicsCategory>();
            var catMap = new Dictionary<string, OnlineGraphicsCategory>(StringComparer.OrdinalIgnoreCase);

            foreach (var c in cat.categories)
            {
                if (c == null || string.IsNullOrEmpty(c.name)) continue;
                if (catMap.TryGetValue(c.name, out var existing))
                {
                    if (c.items != null && existing.items != null)
                    {
                        var existingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var it in existing.items)
                        {
                            if (it != null && !string.IsNullOrEmpty(it.id)) existingIds.Add(it.id);
                        }
                        foreach (var it in c.items)
                        {
                            if (it != null && !string.IsNullOrEmpty(it.id) && !existingIds.Contains(it.id))
                            {
                                existing.items.Add(it);
                                existingIds.Add(it.id);
                            }
                        }
                    }
                }
                else
                {
                    if (c.items == null) c.items = new List<OnlineGraphicItem>();
                    merged.Add(c);
                    catMap[c.name] = c;
                }
            }
            cat.categories = merged;
            return cat;
        }

        public static void ClearMemoryForOffline()
        {
            lock (_cacheLock)
            {
                IsOffline = true;
                IsLoading = false;
                _catalog = null;
                _ramDataCache.Clear();
                _downloadingImages.Clear();
            }
        }

        public static OnlineGraphicsResponse GetCatalogCached()
        {
            if (!CheckInternetConnected())
            {
                ClearMemoryForOffline();
                return null;
            }
            return _catalog;
        }

        public static OnlineGraphicItem FindItem(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return null;
            var cat = GetCatalogCached();
            if (cat == null || cat.categories == null) return null;
            string idClean = identifier.Trim();
            foreach (var c in cat.categories)
            {
                if (c.items == null) continue;
                foreach (var it in c.items)
                {
                    if (string.Equals(it.id, idClean, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(it.name, idClean, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(it.url) && it.url.EndsWith("/" + idClean, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(it.url) && it.url.EndsWith("/" + idClean + ".svg", StringComparison.OrdinalIgnoreCase)))
                    {
                        return it;
                    }
                }
            }
            return null;
        }

        public static void RefreshCatalogAsync(string apiUrl = "https://raw.githubusercontent.com/thedanh6112009-collab/zenq-graphics-api/main/data/graphics.json")
        {
            Task.Run(async () =>
            {
                TempCleanupManager.PurgeAllCloudCachesOnDisk();

                if (!CheckInternetConnected())
                {
                    ClearMemoryForOffline();
                    try { OnCatalogUpdated?.Invoke(null); } catch { }
                    return;
                }

                IsLoading = true;
                try
                {
                    string json = null;
                    string cacheBuster = "?_t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    try
                    {
                        json = await _httpClient.GetStringAsync(apiUrl + cacheBuster).ConfigureAwait(false);
                    }
                    catch
                    {
                        string githubRawUrl = "https://raw.githubusercontent.com/thedanh6112009-collab/zenq-graphics-api/main/data/graphics.json" + cacheBuster;
                        json = await _httpClient.GetStringAsync(githubRawUrl).ConfigureAwait(false);
                    }

                    if (!string.IsNullOrEmpty(json))
                    {
                        var newCatalog = JsonSerializer.Deserialize<OnlineGraphicsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (newCatalog != null && newCatalog.categories != null && newCatalog.categories.Count > 0)
                        {
                            lock (_cacheLock)
                            {
                                IsOffline = false;
                                IsLoading = false;
                                _catalog = NormalizeCatalogCategories(newCatalog);
                            }
                            try { OnCatalogUpdated?.Invoke(_catalog); } catch { }
                            return;
                        }
                    }

                    ClearMemoryForOffline();
                    try { OnCatalogUpdated?.Invoke(null); } catch { }
                }
                catch
                {
                    ClearMemoryForOffline();
                    try { OnCatalogUpdated?.Invoke(null); } catch { }
                }
                finally
                {
                    IsLoading = false;
                }
            });
        }

        public static async Task<OnlineGraphicsResponse> FetchCatalogAsync(string apiUrl = "https://raw.githubusercontent.com/thedanh6112009-collab/zenq-graphics-api/main/data/graphics.json")
        {
            if (!CheckInternetConnected())
            {
                ClearMemoryForOffline();
                return null;
            }
            if (_catalog != null) return _catalog;

            try
            {
                string cacheBuster = "?_t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string json = await _httpClient.GetStringAsync(apiUrl + cacheBuster).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(json))
                {
                    var parsed = NormalizeCatalogCategories(JsonSerializer.Deserialize<OnlineGraphicsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));
                    lock (_cacheLock)
                    {
                        IsOffline = false;
                        _catalog = parsed;
                    }
                    return _catalog;
                }
            }
            catch
            {
                ClearMemoryForOffline();
            }
            return null;
        }

        private static Bitmap DecodeGraphicBytesToBitmap(byte[] data, string url, int targetSize)
        {
            if (data == null || data.Length == 0) return null;
            try
            {
                bool isSvg = (!string.IsNullOrEmpty(url) && url.EndsWith(".svg", StringComparison.OrdinalIgnoreCase));
                if (!isSvg && data.Length > 5)
                {
                    string head = Encoding.UTF8.GetString(data, 0, Math.Min(256, data.Length)).TrimStart();
                    if (head.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) || head.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
                        isSvg = true;
                }

                if (isSvg)
                {
                    string svgText = Encoding.UTF8.GetString(data);
                    return SvgRasterizer.RenderSvgToBitmap(svgText, targetSize, targetSize);
                }

                using var ms = new MemoryStream(data);
                using var img = Image.FromStream(ms);
                return new Bitmap(img);
            }
            catch
            {
                return null;
            }
        }

        private static async Task<byte[]> DownloadBytesFromCloudAsync(string url)
        {
            if (string.IsNullOrEmpty(url) || !CheckInternetConnected()) return null;
            try
            {
                return await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
            }
            catch
            {
                string githubRawUrl = url.Replace("https://zenq-graphics-api-online.vercel.app/", "https://raw.githubusercontent.com/thedanh6112009-collab/zenq-graphics-api/main/public/");
                return await _httpClient.GetByteArrayAsync(githubRawUrl).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Tải và giải mã đồ họa hoàn toàn trong RAM để hiển thị thumbnail (Tuyệt đối KHÔNG ghi xuống ổ cứng).
        /// </summary>
        public static Bitmap GetCachedOrDownloadImage(string id, string url, int targetSize = 1600)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(url)) return null;
            if (!CheckInternetConnected())
            {
                ClearMemoryForOffline();
                return null;
            }

            byte[] cachedBytes = null;
            lock (_cacheLock)
            {
                _ramDataCache.TryGetValue(id, out cachedBytes);
            }

            if (cachedBytes != null && cachedBytes.Length > 0)
            {
                return DecodeGraphicBytesToBitmap(cachedBytes, url, targetSize);
            }

            lock (_downloadingImages)
            {
                if (_downloadingImages.Contains(id)) return null;
                _downloadingImages.Add(id);
            }

            Task.Run(async () =>
            {
                try
                {
                    byte[] data = await DownloadBytesFromCloudAsync(url).ConfigureAwait(false);
                    if (data != null && data.Length > 0)
                    {
                        lock (_cacheLock)
                        {
                            IsOffline = false;
                            // Giới hạn bộ nhớ đệm RAM tối đa 250 mục nhẹ
                            if (_ramDataCache.Count > 250) _ramDataCache.Clear();
                            _ramDataCache[id] = data;
                        }
                        TriggerImageDownloaded();
                    }
                }
                catch
                {
                    if (!CheckInternetConnected()) ClearMemoryForOffline();
                }
                finally
                {
                    lock (_downloadingImages) { _downloadingImages.Remove(id); }
                }
            });

            return null;
        }

        /// <summary>
        /// CHỈ KHI NGƯỜI DÙNG BẤM CHỌN CHÈN ĐỒ HỌA VECTOR VÀO TAB:
        /// Tải tệp đồ họa về thư mục làm việc tạm của tab (TempCleanupManager.ActiveTabWorkDirectory).
        /// Khi đóng tab đó, tệp trong thư mục làm việc tạm sẽ bị xóa sạch ngay lập tức.
        /// </summary>
        public static (string TempFilePath, Bitmap RenderedBitmap) DownloadGraphicForTabUse(string id, string url, int targetSize = 1600)
        {
            if (string.IsNullOrEmpty(url) || !CheckInternetConnected())
            {
                ClearMemoryForOffline();
                return (null, null);
            }

            try
            {
                byte[] data = null;
                lock (_cacheLock)
                {
                    _ramDataCache.TryGetValue(id ?? "", out data);
                }

                if (data == null || data.Length == 0)
                {
                    data = DownloadBytesFromCloudAsync(url).GetAwaiter().GetResult();
                }

                if (data == null || data.Length == 0)
                {
                    return (null, null);
                }

                string ext = Path.GetExtension(url);
                if (string.IsNullOrEmpty(ext) || ext.Length > 5) ext = ".svg";
                string safeId = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
                foreach (char c in Path.GetInvalidFileNameChars()) safeId = safeId.Replace(c, '_');

                string workDir = TempCleanupManager.ActiveTabWorkDirectory;
                string tempFilePath = Path.Combine(workDir, $"vec_tab_{Guid.NewGuid():N}_{safeId}{ext}");
                File.WriteAllBytes(tempFilePath, data);

                Bitmap bmp = DecodeGraphicBytesToBitmap(data, url, targetSize);
                return (tempFilePath, bmp);
            }
            catch
            {
                if (!CheckInternetConnected()) ClearMemoryForOffline();
                return (null, null);
            }
        }

        public static string GetCachedImagePath(string id, string url)
        {
            string ext = Path.GetExtension(url ?? "");
            if (string.IsNullOrEmpty(ext)) ext = ".svg";
            return Path.Combine(TempCleanupManager.ActiveTabWorkDirectory, $"{id}{ext}");
        }

        public static void CleanupUnreferencedCache(HashSet<string> activeFiles)
        {
            TempCleanupManager.PurgeAllCloudCachesOnDisk();
        }

        public static async Task<Bitmap> GetOrDownloadImageAsync(string id, string url, int targetSize = 1600)
        {
            return await Task.Run(() => GetCachedOrDownloadImage(id, url, targetSize)).ConfigureAwait(false);
        }

        public static void CleanupCache()
        {
            lock (_cacheLock)
            {
                _ramDataCache.Clear();
                _downloadingImages.Clear();
            }
            TempCleanupManager.PurgeAllCloudCachesOnDisk();
        }
    }
}

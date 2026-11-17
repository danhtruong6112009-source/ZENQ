using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZenQOffice.Core
{
    public class OnlineImagesCategory
    {
        public string name { get; set; }
        public string folder { get; set; }
        public List<OnlineImageItem> items { get; set; }
    }

    public class OnlineImageItem
    {
        public string id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string local_path { get; set; }
        public string type { get; set; }
    }

    public class OnlineImagesResponse
    {
        public string version { get; set; }
        public int total { get; set; }
        public List<OnlineImagesCategory> categories { get; set; }
    }

    /// <summary>
    /// Quản lý Kho Hình ảnh Stock 4K 100% Cloud-Only:
    /// - Tuyệt đối KHÔNG tạo hay lưu thư mục Cache\Images trong AppData, không dùng dữ liệu offline trên ổ cứng.
    /// - Khi duyệt danh sách: chỉ tải thumbnail vào RAM.
    /// - Khi mất kết nối mạng: báo trạng thái IsOffline = true ("Mất kết nối Internet").
    /// - Chỉ khi người dùng bấm chọn chèn một ảnh vào tab thì mới tải ảnh đó về thư mục làm việc tạm của tab
    ///   (TempCleanupManager.ActiveTabWorkDirectory), và khi đóng tab sẽ tự động xóa sạch ngay lập tức.
    /// </summary>
    public static class OnlineImagesManager
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private static OnlineImagesResponse _catalog;
        private static readonly Dictionary<string, byte[]> _ramDataCache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _downloadingImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _cacheLock = new object();

        public static bool IsOffline { get; private set; } = false;
        public static bool IsLoading { get; private set; } = false;

        private static System.Threading.Timer _throttleTimer;
        private static readonly object _throttleLock = new object();

        public static event Action<OnlineImagesResponse> OnCatalogUpdated;
        public static event Action OnImageDownloaded;

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
                    }, null, 120, System.Threading.Timeout.Infinite);
                }
            }
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

        public static OnlineImagesResponse GetCatalogCached()
        {
            if (!CheckInternetConnected())
            {
                ClearMemoryForOffline();
                return null;
            }
            return _catalog;
        }

        public static void RefreshCatalogAsync(string catalogUrl = "https://raw.githubusercontent.com/danhtruong6112009-source/ZENQ/master/data/images.json")
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
                    string cacheBuster = "?_t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    string json = await _httpClient.GetStringAsync(catalogUrl + cacheBuster).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(json))
                    {
                        var cat = JsonSerializer.Deserialize<OnlineImagesResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (cat != null && cat.categories != null && cat.categories.Count > 0)
                        {
                            lock (_cacheLock)
                            {
                                IsOffline = false;
                                IsLoading = false;
                                _catalog = cat;
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

        private static Bitmap DecodeScaledBitmapFromBytes(byte[] data, int maxDimension)
        {
            if (data == null || data.Length == 0) return null;
            try
            {
                using var ms = new MemoryStream(data);
                using var orig = Image.FromStream(ms, false, false);
                int w = orig.Width;
                int h = orig.Height;
                if (w <= 0 || h <= 0) return null;

                float scale = Math.Min((float)maxDimension / w, (float)maxDimension / h);
                if (scale >= 1.0f)
                {
                    return new Bitmap(orig);
                }

                int tw = Math.Max(1, (int)(w * scale));
                int th = Math.Max(1, (int)(h * scale));

                Bitmap scaled = new Bitmap(tw, th, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.DrawImage(orig, 0, 0, tw, th);
                }
                return scaled;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Tải và giải mã hình ảnh hoàn toàn trong RAM để hiển thị thumbnail (Tuyệt đối KHÔNG ghi xuống ổ cứng).
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
                return DecodeScaledBitmapFromBytes(cachedBytes, targetSize);
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
                    byte[] data = await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
                    if (data != null && data.Length > 0)
                    {
                        lock (_cacheLock)
                        {
                            IsOffline = false;
                            if (_ramDataCache.Count > 120) _ramDataCache.Clear();
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
        /// CHỈ KHI NGƯỜI DÙNG BẤM CHỌN CHÈN HÌNH ẢNH VÀO TAB:
        /// Tải tệp hình ảnh về thư mục làm việc tạm của tab (TempCleanupManager.ActiveTabWorkDirectory).
        /// Khi đóng tab đó, tệp trong thư mục làm việc tạm sẽ bị xóa sạch ngay lập tức.
        /// </summary>
        public static (string TempFilePath, Bitmap RenderedBitmap) DownloadImageForTabUse(string id, string url, int targetSize = 1920)
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
                    data = _httpClient.GetByteArrayAsync(url).GetAwaiter().GetResult();
                }

                if (data == null || data.Length == 0)
                {
                    return (null, null);
                }

                string ext = ".jpg";
                try
                {
                    Uri uri = new Uri(url);
                    string extCandidate = Path.GetExtension(uri.AbsolutePath);
                    if (!string.IsNullOrEmpty(extCandidate) && extCandidate.Length <= 5)
                        ext = extCandidate;
                }
                catch { }

                string safeId = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
                foreach (char c in Path.GetInvalidFileNameChars()) safeId = safeId.Replace(c, '_');

                string workDir = TempCleanupManager.ActiveTabWorkDirectory;
                string tempFilePath = Path.Combine(workDir, $"img_tab_{Guid.NewGuid():N}_{safeId}{ext}");
                File.WriteAllBytes(tempFilePath, data);

                Bitmap bmp = DecodeScaledBitmapFromBytes(data, targetSize);
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
            string ext = ".jpg";
            try
            {
                if (!string.IsNullOrEmpty(url))
                {
                    Uri uri = new Uri(url);
                    string extCandidate = Path.GetExtension(uri.AbsolutePath);
                    if (!string.IsNullOrEmpty(extCandidate) && extCandidate.Length <= 5)
                        ext = extCandidate;
                }
            }
            catch { }
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

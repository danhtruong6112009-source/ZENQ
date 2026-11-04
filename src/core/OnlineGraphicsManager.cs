using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
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

    public static class OnlineGraphicsManager
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        private static readonly string _cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZenQOffice", "Cache", "Graphics");
        private static readonly string _catalogCacheFile = Path.Combine(_cacheDir, "catalog.json");
        private static OnlineGraphicsResponse _catalog;
        private static readonly HashSet<string> _downloadingImages = new HashSet<string>();

        public static event Action<OnlineGraphicsResponse> OnCatalogUpdated;
        public static event Action OnImageDownloaded;
        
        private static System.Threading.Timer _throttleTimer;
        private static readonly object _throttleLock = new object();

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

        public static string ResolveLocalAssetPath(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url)) return null;
                Uri parsedUri = new Uri(url);
                string relPath = parsedUri.AbsolutePath.TrimStart('/');
                int pubIdx = relPath.IndexOf("public/", StringComparison.OrdinalIgnoreCase);
                string subPath = pubIdx >= 0 ? relPath.Substring(pubIdx + 7) : relPath;
                subPath = subPath.Replace('/', Path.DirectorySeparatorChar);

                string userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] searchPaths = new string[]
                {
                    Path.Combine(userDir, "Desktop", "zenq-graphics-api", "public", subPath),
                    Path.Combine(userDir, "Desktop", "Bo_Do_Hoa_6_Chu_De_Canva", subPath),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public", subPath)
                };

                foreach (var p in searchPaths)
                {
                    if (File.Exists(p)) return p;
                }

                string fileName = Path.GetFileName(subPath);
                string dirName = Path.GetFileName(Path.GetDirectoryName(subPath) ?? "");
                string[] fallbackSearch = new string[]
                {
                    Path.Combine(userDir, "Desktop", "zenq-graphics-api", "public", dirName, fileName),
                    Path.Combine(userDir, "Desktop", "Bo_Do_Hoa_6_Chu_De_Canva", dirName, fileName)
                };
                foreach (var p in fallbackSearch)
                {
                    if (File.Exists(p)) return p;
                }
            }
            catch { }
            return null;
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

            // Hợp nhất các danh mục có cùng tên chuẩn hóa (ví dụ: gộp toàn bộ vector Việt Nam vào 1 danh mục duy nhất)
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

        public static OnlineGraphicsResponse GetCatalogCached()
        {
            if (_catalog != null) return _catalog;

            try
            {
                if (File.Exists(_catalogCacheFile))
                {
                    string json = File.ReadAllText(_catalogCacheFile);
                    var cat = JsonSerializer.Deserialize<OnlineGraphicsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (cat != null && cat.categories != null && cat.categories.Count > 0)
                    {
                        _catalog = NormalizeCatalogCategories(cat);
                        return _catalog;
                    }
                }

                // Fallback cục bộ từ thư mục ứng dụng và repository
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] candidatePaths = new[]
                {
                    Path.Combine(appDir, "data", "graphics.json"),
                    Path.Combine(appDir, "..", "..", "..", "data", "graphics.json"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "genofficepy", "data", "graphics.json"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "zenq-graphics-api", "data", "graphics.json")
                };

                foreach (string candidate in candidatePaths)
                {
                    if (File.Exists(candidate))
                    {
                        string json = File.ReadAllText(candidate);
                        var cat = JsonSerializer.Deserialize<OnlineGraphicsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (cat != null && cat.categories != null && cat.categories.Count > 0)
                        {
                            _catalog = NormalizeCatalogCategories(cat);
                            try
                            {
                                if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);
                                File.WriteAllText(_catalogCacheFile, json);
                            }
                            catch { }
                            return _catalog;
                        }
                    }
                }
            }
            catch { }

            return null;
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
                try
                {
                    if (!Directory.Exists(_cacheDir))
                        Directory.CreateDirectory(_cacheDir);

                    string json = null;
                    string cacheBuster = "?_t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    try
                    {
                        json = await _httpClient.GetStringAsync(apiUrl + cacheBuster).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Fallback to GitHub raw repository
                        string githubRawUrl = "https://raw.githubusercontent.com/thedanh6112009-collab/zenq-graphics-api/main/data/graphics.json" + cacheBuster;
                        json = await _httpClient.GetStringAsync(githubRawUrl).ConfigureAwait(false);
                    }

                    if (!string.IsNullOrEmpty(json))
                    {
                        var newCatalog = JsonSerializer.Deserialize<OnlineGraphicsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (newCatalog != null && newCatalog.categories != null && newCatalog.categories.Count > 0)
                        {
                            _catalog = NormalizeCatalogCategories(newCatalog);
                            try { File.WriteAllText(_catalogCacheFile, json); } catch { }
                            OnCatalogUpdated?.Invoke(_catalog);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error refreshing online graphics catalog: " + ex.Message);
                }
            });
        }

        public static async Task<OnlineGraphicsResponse> FetchCatalogAsync(string apiUrl = "https://raw.githubusercontent.com/thedanh6112009-collab/zenq-graphics-api/main/data/graphics.json")
        {
            try
            {
                if (_catalog != null) return _catalog;

                if (!Directory.Exists(_cacheDir))
                    Directory.CreateDirectory(_cacheDir);

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
                    _catalog = NormalizeCatalogCategories(JsonSerializer.Deserialize<OnlineGraphicsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));
                    try { File.WriteAllText(_catalogCacheFile, json); } catch { }
                    return _catalog;
                }

                return GetCatalogCached();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching online graphics: " + ex.Message);
                return GetCatalogCached();
            }
        }

        public static Bitmap GetCachedOrDownloadImage(string id, string url, int targetSize = 1600)
        {
            try
            {
                string localPath = GetCachedImagePath(id, url);
                if (File.Exists(localPath))
                {
                    try
                    {
                        if (localPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        {
                            return SvgRasterizer.RenderSvgToBitmap(localPath, targetSize, targetSize);
                        }
                        using (var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            return new Bitmap(fs);
                        }
                    }
                    catch { }
                }

                // Check direct local repo file first for instant zero-latency loading
                try
                {
                    string localAsset = ResolveLocalAssetPath(url);
                    if (!string.IsNullOrEmpty(localAsset) && File.Exists(localAsset))
                    {
                        if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);
                        if (!File.Exists(localPath) || new FileInfo(localPath).Length == 0)
                        {
                            File.Copy(localAsset, localPath, true);
                        }
                        if (localPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        {
                            return SvgRasterizer.RenderSvgToBitmap(localPath, targetSize, targetSize);
                        }
                        using (var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            return new Bitmap(fs);
                        }
                    }
                }
                catch { }

                // If not cached, trigger non-blocking download
                lock (_downloadingImages)
                {
                    if (_downloadingImages.Contains(id)) return null;
                    _downloadingImages.Add(id);
                }

                Task.Run(async () =>
                {
                    try
                    {
                        if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);

                        byte[] data = null;
                        try
                        {
                            data = await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Fallback to GitHub Raw
                            string githubRawUrl = url.Replace("https://zenq-graphics-api-online.vercel.app/", "https://raw.githubusercontent.com/thedanh6112009-collab/zenq-graphics-api/main/public/");
                            data = await _httpClient.GetByteArrayAsync(githubRawUrl).ConfigureAwait(false);
                        }

                        if (data != null && data.Length > 0)
                        {
                            File.WriteAllBytes(localPath, data);
                            TriggerImageDownloaded();
                        }
                    }
                    catch { }
                    finally
                    {
                        lock (_downloadingImages) { _downloadingImages.Remove(id); }
                    }
                });

                return null;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<Bitmap> GetOrDownloadImageAsync(string id, string url)
        {
            try
            {
                if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);

                string localPath = GetCachedImagePath(id, url);

                if (File.Exists(localPath))
                {
                    try
                    {
                        if (localPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        {
                            string svgContent = File.ReadAllText(localPath);
                            return SvgRasterizer.RenderSvgToBitmap(svgContent, 1600, 1600);
                        }
                        using (var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            return new Bitmap(fs);
                        }
                    }
                    catch { /* Corrupted, redownload */ }
                }

                // Check local repo file first
                try
                {
                    string localAsset = ResolveLocalAssetPath(url);
                    if (!string.IsNullOrEmpty(localAsset) && File.Exists(localAsset))
                    {
                        if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);
                        if (!File.Exists(localPath) || new FileInfo(localPath).Length == 0)
                        {
                            File.Copy(localAsset, localPath, true);
                        }
                        if (localPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        {
                            return SvgRasterizer.RenderSvgToBitmap(localPath, 1600, 1600);
                        }
                        using (var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            return new Bitmap(fs);
                        }
                    }
                }
                catch { }

                byte[] data = null;
                try
                {
                    data = await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
                }
                catch
                {
                    string githubRawUrl = url.Replace("https://zenq-graphics-api-online.vercel.app/", "https://raw.githubusercontent.com/thedanh6112009-collab/zenq-graphics-api/main/public/");
                    data = await _httpClient.GetByteArrayAsync(githubRawUrl).ConfigureAwait(false);
                }

                if (data != null && data.Length > 0)
                {
                    File.WriteAllBytes(localPath, data);
                }

                if (localPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    string svgContent = File.ReadAllText(localPath);
                    return SvgRasterizer.RenderSvgToBitmap(svgContent, 512, 512);
                }

                using (var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return new Bitmap(fs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading image {id}: {ex.Message}");
                return null;
            }
        }

        public static string GetCachedImagePath(string id, string url)
        {
            string ext = Path.GetExtension(url);
            if (string.IsNullOrEmpty(ext)) ext = ".png";
            return Path.Combine(_cacheDir, id + ext);
        }

        /// <summary>
        /// Xóa sạch các file đồ họa SVG/PNG tải tạm từ GitHub để giải phóng ổ cứng.
        /// </summary>
        public static void CleanupCache()
        {
            try
            {
                if (Directory.Exists(_cacheDir))
                {
                    var files = Directory.GetFiles(_cacheDir);
                    foreach (var f in files)
                    {
                        if (f.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                        {
                            try { File.Delete(f); } catch { }
                        }
                    }
                }
            }
            catch { }
        }
    }
}
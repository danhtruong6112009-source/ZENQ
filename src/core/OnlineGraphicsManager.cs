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
        
        public static void TriggerImageDownloaded()
        {
            OnImageDownloaded?.Invoke();
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
                    c.name = "Minh họa Hoạt cảnh & Nhân vật";
                }
                else if (n.IndexOf("unDraw", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.name = "Minh họa Chủ đề & Sự vật";
                }
                else if (n.IndexOf("Color Emojis", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.name = "Biểu tượng Đa sắc (Fluent)";
                }
                else if (n.IndexOf("Flags", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.name = "Quốc kỳ các nước";
                }
            }
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

        public static void RefreshCatalogAsync(string apiUrl = "https://zenq-graphics-api-online.vercel.app/api/graphics")
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

        public static async Task<OnlineGraphicsResponse> FetchCatalogAsync(string apiUrl = "https://zenq-graphics-api-online.vercel.app/api/graphics")
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

        public static Bitmap GetCachedOrDownloadImage(string id, string url, int targetSize = 1200)
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
                            string svgContent = File.ReadAllText(localPath);
                            return SvgRasterizer.RenderSvgToBitmap(svgContent, targetSize, targetSize);
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
                    Uri parsedUri = new Uri(url);
                    string relPath = parsedUri.AbsolutePath.TrimStart('/');
                    string localRepoFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "zenq-graphics-api", "public", relPath.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(localRepoFile))
                    {
                        if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);
                        File.Copy(localRepoFile, localPath, true);
                        if (localPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        {
                            string svgContent = File.ReadAllText(localPath);
                            return SvgRasterizer.RenderSvgToBitmap(svgContent, targetSize, targetSize);
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
                            OnImageDownloaded?.Invoke();
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
                    Uri parsedUri = new Uri(url);
                    string relPath = parsedUri.AbsolutePath.TrimStart('/');
                    string localRepoFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "zenq-graphics-api", "public", relPath.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(localRepoFile))
                    {
                        File.Copy(localRepoFile, localPath, true);
                        if (localPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        {
                            string svgContent = File.ReadAllText(localPath);
                            return SvgRasterizer.RenderSvgToBitmap(svgContent, 1600, 1600);
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
    }
}

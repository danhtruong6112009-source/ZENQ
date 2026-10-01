using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GenOffice.Core
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
                        _catalog = cat;
                        return _catalog;
                    }
                }

                // Fallback cục bộ từ thư mục ứng dụng
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string localDataFile = Path.Combine(appDir, "data", "graphics.json");
                if (File.Exists(localDataFile))
                {
                    string json = File.ReadAllText(localDataFile);
                    var cat = JsonSerializer.Deserialize<OnlineGraphicsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (cat != null && cat.categories != null && cat.categories.Count > 0)
                    {
                        _catalog = cat;
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

                    string json = await _httpClient.GetStringAsync(apiUrl).ConfigureAwait(false);
                    var newCatalog = JsonSerializer.Deserialize<OnlineGraphicsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (newCatalog != null && newCatalog.categories != null)
                    {
                        _catalog = newCatalog;
                        try { File.WriteAllText(_catalogCacheFile, json); } catch { }
                        OnCatalogUpdated?.Invoke(_catalog);
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

                string json = await _httpClient.GetStringAsync(apiUrl).ConfigureAwait(false);
                _catalog = JsonSerializer.Deserialize<OnlineGraphicsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                try { File.WriteAllText(_catalogCacheFile, json); } catch { }
                return _catalog;
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
                        byte[] data = await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
                        File.WriteAllBytes(localPath, data);
                        OnImageDownloaded?.Invoke();
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

                byte[] data = await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
                File.WriteAllBytes(localPath, data);

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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
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

    public static class OnlineImagesManager
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private static readonly string _cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZenQOffice", "Cache", "Images");
        private static readonly string _catalogCacheFile = Path.Combine(_cacheDir, "catalog.json");
        private static OnlineImagesResponse _catalog;
        private static readonly HashSet<string> _downloadingImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static event Action<OnlineImagesResponse> OnCatalogUpdated;
        public static event Action OnImageDownloaded;

        public static void TriggerImageDownloaded()
        {
            OnImageDownloaded?.Invoke();
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
            return Path.Combine(_cacheDir, id + ext);
        }

        public static string FindLocalSourceFile(string url, string localPathHint = null)
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            List<string> candidateRelPaths = new List<string>();

            if (!string.IsNullOrEmpty(localPathHint))
            {
                candidateRelPaths.Add(localPathHint.Replace('/', Path.DirectorySeparatorChar));
            }

            try
            {
                if (!string.IsNullOrEmpty(url))
                {
                    Uri parsedUri = new Uri(url);
                    string segs = parsedUri.AbsolutePath.TrimStart('/');
                    // E.g. master/img/bau-troi/file.jpg or img/bau-troi/file.jpg
                    int imgIdx = segs.IndexOf("img/", StringComparison.OrdinalIgnoreCase);
                    if (imgIdx >= 0)
                    {
                        string sub = segs.Substring(imgIdx).Replace('/', Path.DirectorySeparatorChar);
                        candidateRelPaths.Add(sub);
                    }
                }
            }
            catch { }

            string[] baseSearchDirs = new[]
            {
                appDir,
                Path.Combine(appDir, "..", "..", ".."),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "genofficepy")
            };

            foreach (var bDir in baseSearchDirs)
            {
                foreach (var rel in candidateRelPaths)
                {
                    string candidate = Path.Combine(bDir, rel);
                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
            }

            return null;
        }

        public static OnlineImagesResponse GetCatalogCached()
        {
            if (_catalog != null) return _catalog;

            try
            {
                // 1. Kiểm tra cache tệp catalog tại AppData
                if (File.Exists(_catalogCacheFile))
                {
                    string json = File.ReadAllText(_catalogCacheFile);
                    var cat = JsonSerializer.Deserialize<OnlineImagesResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (cat != null && cat.categories != null && cat.categories.Count > 0)
                    {
                        _catalog = cat;
                        return _catalog;
                    }
                }

                // 2. Fallback cục bộ từ repo hoặc thư mục ứng dụng
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] candidatePaths = new[]
                {
                    Path.Combine(appDir, "data", "images.json"),
                    Path.Combine(appDir, "..", "..", "..", "data", "images.json"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "genofficepy", "data", "images.json"),
                    Path.Combine(appDir, "img", "catalog.json"),
                    Path.Combine(appDir, "..", "..", "..", "img", "catalog.json"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "genofficepy", "img", "catalog.json")
                };

                foreach (string candidate in candidatePaths)
                {
                    if (File.Exists(candidate))
                    {
                        string json = File.ReadAllText(candidate);
                        var cat = JsonSerializer.Deserialize<OnlineImagesResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
            }
            catch { }

            return null;
        }

        public static void RefreshCatalogAsync(string catalogUrl = "https://raw.githubusercontent.com/danhtruong6112009-source/ZENQ/master/data/images.json")
        {
            Task.Run(async () =>
            {
                try
                {
                    if (!Directory.Exists(_cacheDir))
                        Directory.CreateDirectory(_cacheDir);

                    string cacheBuster = "?_t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    string json = await _httpClient.GetStringAsync(catalogUrl + cacheBuster).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(json))
                    {
                        var cat = JsonSerializer.Deserialize<OnlineImagesResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (cat != null && cat.categories != null && cat.categories.Count > 0)
                        {
                            _catalog = cat;
                            File.WriteAllText(_catalogCacheFile, json);
                            OnCatalogUpdated?.Invoke(_catalog);
                        }
                    }
                }
                catch { }
            });
        }

        public static Bitmap GetCachedOrDownloadImage(string id, string url, int targetSize = 1600)
        {
            try
            {
                string localPath = GetCachedImagePath(id, url);

                // 1. Kiểm tra cache đã tải trước đó
                if (File.Exists(localPath))
                {
                    try
                    {
                        using (var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            return new Bitmap(fs);
                        }
                    }
                    catch { }
                }

                // 2. Kiểm tra tệp nguồn cục bộ trong thư mục img/ của dự án (Zero network latency)
                string localSource = FindLocalSourceFile(url);
                if (!string.IsNullOrEmpty(localSource) && File.Exists(localSource))
                {
                    try
                    {
                        if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);
                        File.Copy(localSource, localPath, true);
                        using (var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            return new Bitmap(fs);
                        }
                    }
                    catch { }
                }

                // 3. Nếu chưa có, kích hoạt tải ngầm từ GitHub raw
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
    }
}

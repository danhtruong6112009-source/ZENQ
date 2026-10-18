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
        private static readonly string _thumbDir = Path.Combine(_cacheDir, "Thumbs");
        private static readonly string _catalogCacheFile = Path.Combine(_cacheDir, "catalog.json");
        private static OnlineImagesResponse _catalog;
        private static readonly HashSet<string> _downloadingImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static System.Threading.Timer _throttleTimer;
        private static readonly object _throttleLock = new object();

        public static event Action<OnlineImagesResponse> OnCatalogUpdated;
        public static event Action OnImageDownloaded;

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
                    string json = File.ReadAllText(_catalogCacheFile, System.Text.Encoding.UTF8);
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
                        string json = File.ReadAllText(candidate, System.Text.Encoding.UTF8);
                        var cat = JsonSerializer.Deserialize<OnlineImagesResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (cat != null && cat.categories != null && cat.categories.Count > 0)
                        {
                            _catalog = cat;
                            try
                            {
                                if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);
                                File.WriteAllText(_catalogCacheFile, json, System.Text.Encoding.UTF8);
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
                string localSource = FindLocalSourceFile(url);
                string existingSource = File.Exists(localPath) ? localPath : (File.Exists(localSource) ? localSource : null);

                // TRƯỜNG HỢP 1: Yêu cầu hình thu nhỏ (Thumbnail cho Task Pane, targetSize <= 300)
                if (targetSize <= 300)
                {
                    if (!Directory.Exists(_thumbDir)) Directory.CreateDirectory(_thumbDir);
                    string thumbPath = Path.Combine(_thumbDir, string.Format("{0}_{1}.jpg", id, targetSize));

                    // 1a. Kiểm tra tệp thumbnail đã tạo sẵn trong cache
                    if (File.Exists(thumbPath))
                    {
                        try
                        {
                            using (var fs = new FileStream(thumbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                return new Bitmap(fs);
                            }
                        }
                        catch { }
                    }

                    // 1b. Nếu có tệp nguồn cục bộ, tạo ngay thumbnail nhỏ nhẹ và lưu lại
                    if (!string.IsNullOrEmpty(existingSource))
                    {
                        try
                        {
                            Bitmap thumb = GenerateAndSaveThumbnail(existingSource, thumbPath, targetSize);
                            if (thumb != null) return thumb;
                        }
                        catch { }
                    }

                    // 1c. Chưa có cả nguồn lẫn thumb -> Tải nguồn từ mạng ngầm rồi sinh thumbnail
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
                                try
                                {
                                    GenerateAndSaveThumbnail(localPath, thumbPath, targetSize);
                                }
                                catch { }
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

                // TRƯỜNG HỢP 2: Yêu cầu ảnh kích thước lớn (chèn vào Slide)
                if (!string.IsNullOrEmpty(existingSource))
                {
                    try
                    {
                        return LoadScaledImage(existingSource, targetSize);
                    }
                    catch { }
                }

                // Nếu chưa có tệp ảnh gốc, kích hoạt tải ngầm
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

        private static Bitmap GenerateAndSaveThumbnail(string sourcePath, string thumbPath, int targetSize)
        {
            try
            {
                using (var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var orig = Image.FromStream(fs, false, false))
                {
                    int w = orig.Width;
                    int h = orig.Height;
                    if (w <= 0 || h <= 0) return null;

                    float scale = Math.Min((float)targetSize / w, (float)targetSize / h);
                    int tw = Math.Max(1, (int)(w * scale));
                    int th = Math.Max(1, (int)(h * scale));

                    Bitmap thumb = new Bitmap(tw, th, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    using (Graphics g = Graphics.FromImage(thumb))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
                        g.DrawImage(orig, 0, 0, tw, th);
                    }

                    try
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(thumbPath)))
                            Directory.CreateDirectory(Path.GetDirectoryName(thumbPath));
                        thumb.Save(thumbPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                    catch { }

                    return thumb;
                }
            }
            catch
            {
                return null;
            }
        }

        private static Bitmap LoadScaledImage(string filePath, int maxDimension)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var orig = Image.FromStream(fs, false, false))
                {
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
            }
            catch
            {
                return null;
            }
        }
    }
}

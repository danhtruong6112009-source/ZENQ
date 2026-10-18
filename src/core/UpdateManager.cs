using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ZenQOffice.Core
{
    public class ReleaseInfo
    {
        public string TagName { get; set; }
        public Version Version { get; set; }
        public string Title { get; set; }
        public string Changelog { get; set; }
        public DateTime PublishedAt { get; set; }
        public string DownloadUrl { get; set; }
        public string DownloadFileName { get; set; }
        public long AssetSize { get; set; }
        public string HtmlUrl { get; set; }
        public bool HasUpdate { get; set; }
        public string ErrorMessage { get; set; }

        public ReleaseInfo()
        {
            TagName = "";
            Title = "";
            Changelog = "";
            DownloadUrl = "";
            DownloadFileName = "ZenQOffice.exe";
            HtmlUrl = "";
            HasUpdate = false;
        }
    }

    public static class UpdateManager
    {
        public const string CURRENT_VERSION = "5.1.0";
        public const string CURRENT_VERSION_TAG = "v5.1";
        public const string GITHUB_OWNER = "danhtruong6112009-source";
        public const string GITHUB_REPO = "ZENQ";

        public static string GitHubReleasesWebUrl
        {
            get { return string.Format("https://github.com/{0}/{1}/releases", GITHUB_OWNER, GITHUB_REPO); }
        }

        public static string GitHubApiLatestUrl
        {
            get { return string.Format("https://api.github.com/repos/{0}/{1}/releases/latest", GITHUB_OWNER, GITHUB_REPO); }
        }

        // Cached last check result
        public static ReleaseInfo LastReleaseInfo { get; private set; }
        public static bool IsChecking { get; private set; }
        public static event Action<ReleaseInfo> UpdateCheckCompleted;

        static UpdateManager()
        {
            try
            {
                // Enable TLS 1.2 (SecurityProtocolType 3072) for modern HTTPS on Windows .NET 4.0 / 4.5
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | SecurityProtocolType.Tls;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            }
            catch { }
        }

        /// <summary>
        /// Kiểm tra bản cập nhật mới từ GitHub Releases trong luồng ngầm
        /// </summary>
        public static void CheckForUpdateAsync(Action<ReleaseInfo> callback = null)
        {
            if (IsChecking) return;
            IsChecking = true;

            ThreadPool.QueueUserWorkItem(delegate
            {
                ReleaseInfo info = CheckForUpdateSync();
                LastReleaseInfo = info;
                IsChecking = false;

                if (callback != null)
                {
                    try { callback(info); } catch { }
                }

                if (UpdateCheckCompleted != null)
                {
                    try { UpdateCheckCompleted(info); } catch { }
                }
            });
        }

        /// <summary>
        /// Kiểm tra cập nhật đồng bộ (Sync)
        /// </summary>
        public static ReleaseInfo CheckForUpdateSync()
        {
            ReleaseInfo info = new ReleaseInfo();

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GitHubApiLatestUrl);
                request.Method = "GET";
                request.UserAgent = "ZenQOffice-AutoUpdater";
                request.Accept = "application/vnd.github.v3+json";
                request.Timeout = 12000;
                request.ReadWriteTimeout = 12000;

                string jsonResponse = null;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                        {
                            jsonResponse = reader.ReadToEnd();
                        }
                    }
                }

                if (string.IsNullOrEmpty(jsonResponse))
                {
                    info.ErrorMessage = "Không nhận được dữ liệu từ GitHub.";
                    return info;
                }

                // Parse JSON using JsonLite
                object parsed = JsonLite.Parse(jsonResponse);
                Dictionary<string, object> root = parsed as Dictionary<string, object>;
                if (root == null)
                {
                    info.ErrorMessage = "Dữ liệu phản hồi không hợp lệ.";
                    return info;
                }

                info.TagName = root.ContainsKey("tag_name") && root["tag_name"] != null ? root["tag_name"].ToString() : "";
                info.Title = root.ContainsKey("name") && root["name"] != null ? root["name"].ToString() : info.TagName;
                info.Changelog = root.ContainsKey("body") && root["body"] != null ? root["body"].ToString() : "";
                info.HtmlUrl = root.ContainsKey("html_url") && root["html_url"] != null ? root["html_url"].ToString() : GitHubReleasesWebUrl;

                if (root.ContainsKey("published_at") && root["published_at"] != null)
                {
                    DateTime pub;
                    if (DateTime.TryParse(root["published_at"].ToString(), out pub))
                    {
                        info.PublishedAt = pub;
                    }
                }

                // Parse assets with intelligent multi-priority scoring
                if (root.ContainsKey("assets") && root["assets"] is List<object>)
                {
                    List<object> assets = root["assets"] as List<object>;
                    string bestUrl = "";
                    string bestName = "";
                    long bestSize = 0;
                    int bestScore = -1;

                    foreach (object item in assets)
                    {
                        Dictionary<string, object> asset = item as Dictionary<string, object>;
                        if (asset == null) continue;

                        string assetName = asset.ContainsKey("name") && asset["name"] != null ? asset["name"].ToString() : "";
                        string downloadUrl = asset.ContainsKey("browser_download_url") && asset["browser_download_url"] != null ? asset["browser_download_url"].ToString() : "";
                        long size = 0;
                        if (asset.ContainsKey("size") && asset["size"] != null)
                        {
                            long.TryParse(asset["size"].ToString(), out size);
                        }

                        string lower = assetName.ToLowerInvariant();
                        int score = 0;

                        // Ưu tiên 1: Tệp cài đặt Setup (.exe chứa setup hoặc install)
                        if (lower.EndsWith(".exe") && (lower.Contains("setup") || lower.Contains("install")))
                        {
                            score = 100;
                        }
                        // Ưu tiên 2: Tệp ZenQOffice.exe
                        else if (lower == "genoffice.exe")
                        {
                            score = 90;
                        }
                        // Ưu tiên 3: File .msi cài đặt
                        else if (lower.EndsWith(".msi"))
                        {
                            score = 85;
                        }
                        // Ưu tiên 4: Bất kỳ file .exe nào khác
                        else if (lower.EndsWith(".exe"))
                        {
                            score = 80;
                        }
                        // Ưu tiên 5: File nén .zip hoặc .rar hoặc .7z
                        else if (lower.EndsWith(".zip") || lower.EndsWith(".rar") || lower.EndsWith(".7z"))
                        {
                            score = 60;
                        }
                        else
                        {
                            score = 10;
                        }

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestUrl = downloadUrl;
                            bestName = assetName;
                            bestSize = size;
                        }
                    }

                    if (!string.IsNullOrEmpty(bestUrl))
                    {
                        info.DownloadUrl = bestUrl;
                        info.DownloadFileName = bestName;
                        info.AssetSize = bestSize;
                    }
                }

                // If no asset was attached, fallback to html_url
                if (string.IsNullOrEmpty(info.DownloadUrl))
                {
                    info.DownloadUrl = info.HtmlUrl;
                }

                // Version Comparison
                Version currentV = ParseVersion(CURRENT_VERSION);
                Version remoteV = ParseVersion(info.TagName);
                info.Version = remoteV;

                if (remoteV != null && currentV != null)
                {
                    info.HasUpdate = (remoteV > currentV);
                }
                else if (!string.IsNullOrEmpty(info.TagName) && !string.Equals(info.TagName.TrimStart('v', 'V'), CURRENT_VERSION.TrimStart('v', 'V'), StringComparison.OrdinalIgnoreCase))
                {
                    info.HasUpdate = true;
                }
                else
                {
                    info.HasUpdate = false;
                }

                if (!info.HasUpdate)
                {
                    info.TagName = CURRENT_VERSION_TAG;
                    info.ErrorMessage = null;
                }
            }
            catch (WebException webEx)
            {
                if (webEx.Response is HttpWebResponse)
                {
                    HttpWebResponse res = (HttpWebResponse)webEx.Response;
                    if (res.StatusCode == HttpStatusCode.NotFound)
                    {
                        // Máy chủ không có bản phát hành nào mới hơn -> Ứng dụng đang ở phiên bản mới nhất
                        info.TagName = CURRENT_VERSION_TAG;
                        info.Version = ParseVersion(CURRENT_VERSION);
                        info.HasUpdate = false;
                        info.ErrorMessage = null;
                        return info;
                    }
                }
                // Thông báo mạng chuyên nghiệp, không đề cập kỹ thuật GitHub
                info.TagName = CURRENT_VERSION_TAG;
                info.Version = ParseVersion(CURRENT_VERSION);
                info.HasUpdate = false;
                info.ErrorMessage = "Không thể kết nối đến máy chủ kiểm tra cập nhật. Vui lòng kiểm tra lại kết nối mạng.";
            }
            catch (Exception)
            {
                info.TagName = CURRENT_VERSION_TAG;
                info.Version = ParseVersion(CURRENT_VERSION);
                info.HasUpdate = false;
                info.ErrorMessage = null;
            }

            return info;
        }

        public static Version ParseVersion(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return new Version(0, 0, 0);
            string clean = tag.Trim().TrimStart('v', 'V').Trim();
            
            // Extract numeric components
            string[] parts = clean.Split(new char[] { '.', '-', '_' });
            List<int> numbers = new List<int>();
            for (int i = 0; i < parts.Length; i++)
            {
                int val;
                if (int.TryParse(parts[i], out val))
                {
                    numbers.Add(val);
                }
                else break;
            }

            if (numbers.Count == 0) return new Version(0, 0, 0);
            if (numbers.Count == 1) return new Version(numbers[0], 0, 0);
            if (numbers.Count == 2) return new Version(numbers[0], numbers[1], 0);
            if (numbers.Count == 3) return new Version(numbers[0], numbers[1], numbers[2]);
            return new Version(numbers[0], numbers[1], numbers[2], numbers[3]);
        }

        /// <summary>
        /// Tải xuống bản cập nhật mới và khởi chạy tiến trình ghi đè tự động
        /// </summary>
        public static void DownloadAndApplyUpdate(ReleaseInfo release, Action<int, long, long> progressCallback, Action onComplete, Action<string> onError)
        {
            if (release == null || string.IsNullOrEmpty(release.DownloadUrl))
            {
                if (onError != null) onError("Không tìm thấy đường dẫn tải tệp cập nhật.");
                return;
            }

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), "GenOffice_Update");
                    if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

                    string targetDownloadPath = Path.Combine(tempDir, release.DownloadFileName ?? "GenOffice_new.exe");
                    if (File.Exists(targetDownloadPath))
                    {
                        try { File.Delete(targetDownloadPath); } catch { }
                    }

                    // Download with progress tracking
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(release.DownloadUrl);
                    req.UserAgent = "ZenQOffice-AutoUpdater";
                    req.Timeout = 60000;
                    req.ReadWriteTimeout = 60000;

                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    {
                        long totalBytes = resp.ContentLength;
                        if (totalBytes <= 0 && release.AssetSize > 0) totalBytes = release.AssetSize;

                        using (Stream sIn = resp.GetResponseStream())
                        using (FileStream sOut = new FileStream(targetDownloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            byte[] buffer = new byte[65536];
                            long bytesReadTotal = 0;
                            int bytesRead;

                            while ((bytesRead = sIn.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                sOut.Write(buffer, 0, bytesRead);
                                bytesReadTotal += bytesRead;

                                if (progressCallback != null && totalBytes > 0)
                                {
                                    int pct = (int)((bytesReadTotal * 100) / totalBytes);
                                    progressCallback(pct, bytesReadTotal, totalBytes);
                                }
                            }
                        }
                    }

                    if (onComplete != null)
                    {
                        try { onComplete(); } catch { }
                    }

                    // Build and execute apply_update.bat with intelligent installer handling
                    string currentAppPath = Application.ExecutablePath;
                    string appDir = Path.GetDirectoryName(currentAppPath);
                    string updateScriptPath = Path.Combine(tempDir, "apply_update.bat");
                    string lowerName = (release.DownloadFileName ?? "").ToLowerInvariant();

                    bool isSetupInstaller = lowerName.Contains("setup") || lowerName.Contains("install") || lowerName.EndsWith(".msi");
                    bool isZip = lowerName.EndsWith(".zip");

                    StringBuilder bat = new StringBuilder();
                    bat.AppendLine("@echo off");
                    bat.AppendLine("chcp 65001 >nul");
                    bat.AppendLine("timeout /t 1 /nobreak >nul");
                    bat.AppendLine(":wait_loop");
                    bat.AppendLine("tasklist /fi \"imagename eq ZenQOffice.exe\" 2>nul | find /i \"ZenQOffice.exe\" >nul");
                    bat.AppendLine("if %errorlevel% equ 0 (");
                    bat.AppendLine("    timeout /t 1 /nobreak >nul");
                    bat.AppendLine("    goto wait_loop");
                    bat.AppendLine(")");

                    if (isSetupInstaller)
                    {
                        // Khởi chạy trình cài đặt độc lập ở chế độ ngầm (Silent mode)
                        if (lowerName.EndsWith(".msi"))
                        {
                            bat.AppendLine(string.Format("start \"\" /wait msiexec /i \"{0}\" /qn", targetDownloadPath));
                        }
                        else
                        {
                            // Sử dụng /silent cho bộ cài custom GenOffice-Setup.exe
                            bat.AppendLine(string.Format("start \"\" /wait \"{0}\" /silent", targetDownloadPath));
                        }
                        bat.AppendLine(string.Format("start \"\" \"{0}\"", currentAppPath));
                        bat.AppendLine("(goto) 2>nul & del \"%~f0\"");
                    }
                    else if (isZip)
                    {
                        // Giải nén đè vào thư mục cài đặt rồi chạy lại
                        bat.AppendLine(string.Format("powershell -Command \"Expand-Archive -Path '{0}' -DestinationPath '{1}' -Force\"", targetDownloadPath, appDir));
                        bat.AppendLine(string.Format("start \"\" \"{0}\"", currentAppPath));
                        bat.AppendLine(string.Format("del \"{0}\" >nul 2>&1", targetDownloadPath));
                        bat.AppendLine("(goto) 2>nul & del \"%~f0\"");
                    }
                    else
                    {
                        // Ghi đè file ZenQOffice.exe
                        bat.AppendLine(string.Format("copy /y \"{0}\" \"{1}\" >nul", targetDownloadPath, currentAppPath));
                        bat.AppendLine(string.Format("start \"\" \"{0}\"", currentAppPath));
                        bat.AppendLine(string.Format("del \"{0}\" >nul 2>&1", targetDownloadPath));
                        bat.AppendLine("(goto) 2>nul & del \"%~f0\"");
                    }

                    File.WriteAllText(updateScriptPath, bat.ToString(), Encoding.Default);

                    // Launch updater script in hidden mode
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "cmd.exe";
                    psi.Arguments = string.Format("/c \"{0}\"", updateScriptPath);
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = true;

                    Process.Start(psi);

                    // Exit current application to allow replacement
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    if (onError != null)
                    {
                        onError("Lỗi khi tải hoặc cài đặt bản cập nhật: " + ex.Message);
                    }
                }
            });
        }
    }
}

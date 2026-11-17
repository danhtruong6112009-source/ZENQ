using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZenQOffice.Core
{
    public class ZenQExtensionItem
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public string version { get; set; } = "1.0.0";
        public string description { get; set; } = "";
        public string category { get; set; } = "Thiết kế"; // Thiết kế, Ảnh, Chữ, Dữ liệu, v.v.
        public int authorId { get; set; } = 0;
        public string authorName { get; set; } = "";
        public List<string> targetApps { get; set; } = new List<string> { "Slides" };
        public string actionId { get; set; } = "";
        public int iconIndex { get; set; } = 0;
        public string downloadUrl { get; set; } = "";
        public string localInstallPath { get; set; } = "";
        public bool isInstalled { get; set; } = false;
        public bool isCustom { get; set; } = false;
    }

    /// <summary>
    /// Trình quản lý hệ sinh thái tiện ích mở rộng ZenQ Extension (.zqe)
    /// Hỗ trợ nạp Cloud từ GitHub repo (danhtruong6112009-source/zenq-office-extensions),
    /// cài đặt cục bộ, và phân quyền Creator/Admin đăng tải/xóa tiện ích.
    /// </summary>
    public static class ZenQExtensionManager
    {
        public const string GITHUB_CATALOG_URL = "https://api.github.com/repos/danhtruong6112009-source/zenq-office-extensions/contents/extensions_catalog.json";
        public const string GITHUB_RAW_CATALOG_URL = "https://raw.githubusercontent.com/danhtruong6112009-source/zenq-office-extensions/main/extensions_catalog.json";
        public const string GITHUB_CONTENTS_BASE = "https://api.github.com/repos/danhtruong6112009-source/zenq-office-extensions/contents/extensions";

        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        private static readonly object _syncLock = new object();
        private static List<ZenQExtensionItem> _cachedCatalog = new List<ZenQExtensionItem>();
        private static bool _isLoaded = false;

        public static event Action OnExtensionsChanged;

        public static string ExtensionsLocalDirectory
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenOffice", "Extensions");
                if (!Directory.Exists(dir))
                {
                    try { Directory.CreateDirectory(dir); } catch { }
                }
                return dir;
            }
        }

        public static IReadOnlyList<ZenQExtensionItem> GetAllExtensions()
        {
            lock (_syncLock)
            {
                return _cachedCatalog.ToList();
            }
        }

        public static async Task RefreshCatalogAsync()
        {
            List<ZenQExtensionItem> cloudItems = new List<ZenQExtensionItem>();
            try
            {
                string token = GetGitHubToken();
                using var req = new HttpRequestMessage(HttpMethod.Get, GITHUB_CATALOG_URL);
                req.Headers.UserAgent.ParseAdd("ZenQOffice/1.0");
                req.Headers.Accept.ParseAdd("application/vnd.github.v3.raw");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);
                }

                string json = null;
                try
                {
                    var resp = await _httpClient.SendAsync(req).ConfigureAwait(false);
                    if (resp.IsSuccessStatusCode)
                    {
                        json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                }
                catch { }

                if (string.IsNullOrWhiteSpace(json))
                {
                    // Fallback to raw with cache buster
                    string cacheBuster = "?_t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    using var rawReq = new HttpRequestMessage(HttpMethod.Get, GITHUB_RAW_CATALOG_URL + cacheBuster);
                    rawReq.Headers.UserAgent.ParseAdd("ZenQOffice/1.0");
                    var rawResp = await _httpClient.SendAsync(rawReq).ConfigureAwait(false);
                    if (rawResp.IsSuccessStatusCode)
                    {
                        json = await rawResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                }

                if (!string.IsNullOrWhiteSpace(json))
                {
                    var parsed = JsonSerializer.Deserialize<List<ZenQExtensionItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsed != null)
                    {
                        cloudItems = parsed;
                    }
                }
            }
            catch { }

            // Quét các tiện ích cài đặt cục bộ
            var localMap = ScanInstalledExtensions();

            lock (_syncLock)
            {
                var merged = new Dictionary<string, ZenQExtensionItem>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in cloudItems)
                {
                    if (string.IsNullOrEmpty(item.id)) continue;
                    if (localMap.TryGetValue(item.id, out var localDir))
                    {
                        item.isInstalled = true;
                        item.localInstallPath = localDir;
                    }
                    merged[item.id] = item;
                }

                // Tiện ích chỉ có ở cục bộ mà chưa đẩy lên Cloud
                foreach (var kvp in localMap)
                {
                    if (!merged.ContainsKey(kvp.Key))
                    {
                        var localMeta = LoadLocalManifest(kvp.Value);
                        if (localMeta != null)
                        {
                            localMeta.isInstalled = true;
                            localMeta.localInstallPath = kvp.Value;
                            merged[kvp.Key] = localMeta;
                        }
                    }
                }

                _cachedCatalog = merged.Values.ToList();
                _isLoaded = true;
            }

            OnExtensionsChanged?.Invoke();
        }

        private static Dictionary<string, string> ScanInstalledExtensions()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string baseDir = ExtensionsLocalDirectory;
                if (!Directory.Exists(baseDir)) return result;

                foreach (var subDir in Directory.GetDirectories(baseDir))
                {
                    string manifestPath = Path.Combine(subDir, "manifest.json");
                    if (File.Exists(manifestPath))
                    {
                        string id = Path.GetFileName(subDir);
                        try
                        {
                            string json = File.ReadAllText(manifestPath);
                            using var doc = JsonDocument.Parse(json);
                            if (doc.RootElement.TryGetProperty("id", out var idProp) && !string.IsNullOrEmpty(idProp.GetString()))
                            {
                                id = idProp.GetString();
                            }
                        }
                        catch { }
                        result[id] = subDir;
                    }
                }
            }
            catch { }
            return result;
        }

        private static ZenQExtensionItem LoadLocalManifest(string extDir)
        {
            try
            {
                string manifestPath = Path.Combine(extDir, "manifest.json");
                if (!File.Exists(manifestPath)) return null;
                string json = File.ReadAllText(manifestPath);
                return JsonSerializer.Deserialize<ZenQExtensionItem>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        public static async Task<(bool Success, string Error)> InstallExtensionAsync(ZenQExtensionItem item)
        {
            if (item == null) return (false, AppL10n.T("Tiện ích không hợp lệ.", "Invalid extension."));
            if (string.IsNullOrEmpty(item.downloadUrl)) return (false, AppL10n.T("Không tìm thấy liên kết tải về.", "Download link not found."));

            try
            {
                string tempZip = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zqe");
                using (var resp = await _httpClient.GetAsync(item.downloadUrl).ConfigureAwait(false))
                {
                    if (!resp.IsSuccessStatusCode)
                    {
                        return (false, string.Format(AppL10n.T("Tải gói thất bại: Mã HTTP {0}", "Package download failed: HTTP {0}"), (int)resp.StatusCode));
                    }
                    var bytes = await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    await File.WriteAllBytesAsync(tempZip, bytes).ConfigureAwait(false);
                }

                string installDir = Path.Combine(ExtensionsLocalDirectory, item.id);
                if (Directory.Exists(installDir))
                {
                    try { Directory.Delete(installDir, true); } catch { }
                }
                Directory.CreateDirectory(installDir);

                ZipFile.ExtractToDirectory(tempZip, installDir, true);

                try { File.Delete(tempZip); } catch { }

                lock (_syncLock)
                {
                    item.isInstalled = true;
                    item.localInstallPath = installDir;
                }

                OnExtensionsChanged?.Invoke();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, AppL10n.T("Lỗi cài đặt tiện ích: ", "Extension install error: ") + ex.Message);
            }
        }

        public static async Task<(bool Success, string Error, ZenQExtensionItem InstalledItem)> InstallFromZqeFileAsync(string zqeFilePath)
        {
            if (string.IsNullOrEmpty(zqeFilePath) || !File.Exists(zqeFilePath))
            {
                return (false, AppL10n.T("Tệp .zqe không tồn tại.", ".zqe file does not exist."), null);
            }

            try
            {
                using var archive = ZipFile.OpenRead(zqeFilePath);
                var manifestEntry = archive.GetEntry("manifest.json");
                if (manifestEntry == null)
                {
                    return (false, AppL10n.T("Gói tiện ích .zqe không hợp lệ (thiếu tệp manifest.json).", "Invalid .zqe package (missing manifest.json)."), null);
                }

                ZenQExtensionItem meta = null;
                using (var ms = new StreamReader(manifestEntry.Open(), Encoding.UTF8))
                {
                    string json = await ms.ReadToEndAsync().ConfigureAwait(false);
                    meta = JsonSerializer.Deserialize<ZenQExtensionItem>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                if (meta == null || string.IsNullOrWhiteSpace(meta.id))
                {
                    return (false, AppL10n.T("Dữ liệu manifest.json không hợp lệ.", "Invalid manifest.json data."), null);
                }

                string installDir = Path.Combine(ExtensionsLocalDirectory, meta.id);
                if (Directory.Exists(installDir))
                {
                    try { Directory.Delete(installDir, true); } catch { }
                }
                Directory.CreateDirectory(installDir);

                archive.ExtractToDirectory(installDir, true);

                meta.isInstalled = true;
                meta.localInstallPath = installDir;

                lock (_syncLock)
                {
                    int idx = _cachedCatalog.FindIndex(x => string.Equals(x.id, meta.id, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0)
                    {
                        _cachedCatalog[idx] = meta;
                    }
                    else
                    {
                        _cachedCatalog.Add(meta);
                    }
                }

                OnExtensionsChanged?.Invoke();
                return (true, null, meta);
            }
            catch (Exception ex)
            {
                return (false, AppL10n.T("Lỗi giải nén gói .zqe: ", "Error extracting .zqe package: ") + ex.Message, null);
            }
        }

        public static (bool Success, string Error) UninstallExtension(string extensionId)
        {
            if (string.IsNullOrEmpty(extensionId)) return (false, AppL10n.T("Mã tiện ích không hợp lệ.", "Invalid extension ID."));
            try
            {
                string installDir = Path.Combine(ExtensionsLocalDirectory, extensionId);
                if (Directory.Exists(installDir))
                {
                    Directory.Delete(installDir, true);
                }

                lock (_syncLock)
                {
                    var found = _cachedCatalog.FirstOrDefault(x => string.Equals(x.id, extensionId, StringComparison.OrdinalIgnoreCase));
                    if (found != null)
                    {
                        found.isInstalled = false;
                        found.localInstallPath = "";
                    }
                }

                OnExtensionsChanged?.Invoke();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, AppL10n.T("Không thể gỡ cài đặt tiện ích: ", "Unable to uninstall extension: ") + ex.Message);
            }
        }

        /// <summary>
        /// Đăng tải tiện ích .zqe lên GitHub Hub (Yêu cầu quyền Creator / Admin và không bị Ban)
        /// </summary>
        public static async Task<(bool Success, string Error)> PublishExtensionAsync(ZenQExtensionItem meta, string zqeSourcePath)
        {
            var user = DatabaseAuthService.CurrentUser;
            if (user == null)
            {
                return (false, AppL10n.T("Vui lòng đăng nhập tài khoản để thực hiện đăng tải tiện ích.", "Please sign in to publish an extension."));
            }

            if (user.Ban)
            {
                return (false, AppL10n.T("Tài khoản của bạn đã bị khóa tính năng. Không thể đăng tải tiện ích.", "Your account is restricted and cannot publish extensions."));
            }

            if (!user.IsCreator && !user.IsAdmin)
            {
                return (false, AppL10n.T("Bạn cần vai trò 'Nhà sáng tạo' (Creator) hoặc 'Quản trị viên' (Admin) để đăng tải tiện ích.", "Creator or Admin role is required to publish extensions."));
            }

            if (meta == null || string.IsNullOrWhiteSpace(meta.name))
            {
                return (false, AppL10n.T("Thông tin tiện ích không được để trống.", "Extension metadata cannot be empty."));
            }

            string token = GetGitHubToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, AppL10n.T("Không tìm thấy quyền chứng thực kho tiện ích (GitHub Token).", "Missing extension hub authorization (GitHub Token)."));
            }

            try
            {
                if (string.IsNullOrWhiteSpace(meta.id))
                {
                    meta.id = "ext." + Guid.NewGuid().ToString("N").Substring(0, 10);
                }

                meta.authorId = user.Id;
                meta.authorName = user.FullName ?? user.Username;

                string cleanFileName = meta.id.Replace(" ", "_") + ".zqe";
                meta.downloadUrl = $"https://raw.githubusercontent.com/danhtruong6112009-source/zenq-office-extensions/main/extensions/{cleanFileName}";

                byte[] zqeBytes = null;
                if (!string.IsNullOrEmpty(zqeSourcePath) && File.Exists(zqeSourcePath))
                {
                    zqeBytes = await File.ReadAllBytesAsync(zqeSourcePath).ConfigureAwait(false);
                }
                else
                {
                    // Tự động đóng gói một file zqe đơn giản chứa manifest.json
                    using var ms = new MemoryStream();
                    using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                    {
                        var entry = zip.CreateEntry("manifest.json");
                        using (var writer = new StreamWriter(entry.Open(), Encoding.UTF8))
                        {
                            writer.Write(JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));
                        }
                    }
                    zqeBytes = ms.ToArray();
                }

                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ZenQOffice-Creator/1.0");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");

                // 1. Tải lên tệp .zqe
                string fileApiUrl = $"{GITHUB_CONTENTS_BASE}/{cleanFileName}";
                string sha = await GetGitHubFileShaAsync(client, fileApiUrl).ConfigureAwait(false);

                var putObj = new Dictionary<string, object>
                {
                    ["message"] = $"Publish extension {meta.name} ({meta.id}) by {meta.authorName}",
                    ["content"] = Convert.ToBase64String(zqeBytes),
                    ["branch"] = "main"
                };
                if (!string.IsNullOrEmpty(sha)) putObj["sha"] = sha;

                using (var content = new StringContent(JsonSerializer.Serialize(putObj), Encoding.UTF8, "application/json"))
                {
                    var putResp = await client.PutAsync(fileApiUrl, content).ConfigureAwait(false);
                    if (!putResp.IsSuccessStatusCode)
                    {
                        string errText = await putResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return (false, string.Format(AppL10n.T("Tải tệp .zqe lên Cloud thất bại: {0} - {1}", "Cloud upload failed: {0} - {1}"), putResp.StatusCode, errText));
                    }
                }

                // 2. Cập nhật extensions_catalog.json trên GitHub
                await UpdateCloudCatalogJsonAsync(client, meta).ConfigureAwait(false);

                // Cập nhật bộ nhớ đệm
                await RefreshCatalogAsync().ConfigureAwait(false);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, AppL10n.T("Lỗi đăng tải tiện ích: ", "Error publishing extension: ") + ex.Message);
            }
        }

        /// <summary>
        /// Xóa tiện ích khỏi Cloud (Chỉ chính tác giả Creator hoặc Admin mới có quyền xóa)
        /// </summary>
        public static async Task<(bool Success, string Error)> DeleteExtensionFromCloudAsync(ZenQExtensionItem item)
        {
            var user = DatabaseAuthService.CurrentUser;
            if (user == null)
            {
                return (false, AppL10n.T("Vui lòng đăng nhập tài khoản để thực hiện thao tác.", "Please sign in to continue."));
            }

            if (user.Ban)
            {
                return (false, AppL10n.T("Tài khoản của bạn đã bị khóa. Không thể thực hiện thao tác này.", "Your account is locked."));
            }

            if (item == null) return (false, AppL10n.T("Tiện ích không tồn tại.", "Extension not found."));

            // Kiểm tra phân quyền: Tác giả hoặc Admin
            bool canDelete = user.IsAdmin || (item.authorId > 0 && item.authorId == user.Id);
            if (!canDelete)
            {
                return (false, AppL10n.T("Bạn chỉ có quyền xóa tiện ích do chính mình đăng tải.", "You can only delete extensions you published."));
            }

            string token = GetGitHubToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, AppL10n.T("Không tìm thấy quyền chứng thực kho tiện ích (GitHub Token).", "Missing extension hub authorization (GitHub Token)."));
            }

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ZenQOffice-Admin/1.0");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");

                // 1. Xóa file .zqe
                string cleanFileName = item.id.Replace(" ", "_") + ".zqe";
                string fileApiUrl = $"{GITHUB_CONTENTS_BASE}/{cleanFileName}";
                await DeleteGitHubFileIfExistsAsync(client, fileApiUrl, $"Delete extension {item.id}").ConfigureAwait(false);

                // 2. Xóa khỏi extensions_catalog.json
                await RemoveFromCloudCatalogJsonAsync(client, item.id).ConfigureAwait(false);

                // 3. Xóa bản cài cục bộ nếu có
                UninstallExtension(item.id);

                await RefreshCatalogAsync().ConfigureAwait(false);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, AppL10n.T("Lỗi khi xóa tiện ích trên Cloud: ", "Error deleting extension on Cloud: ") + ex.Message);
            }
        }

        private static async Task UpdateCloudCatalogJsonAsync(HttpClient client, ZenQExtensionItem meta)
        {
            try
            {
                string catalogSha = await GetGitHubFileShaAsync(client, GITHUB_CATALOG_URL).ConfigureAwait(false);
                List<ZenQExtensionItem> catalog = new List<ZenQExtensionItem>();

                var getResp = await client.GetAsync(GITHUB_CATALOG_URL).ConfigureAwait(false);
                if (getResp.IsSuccessStatusCode)
                {
                    string getJson = await getResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using var doc = JsonDocument.Parse(getJson);
                    if (doc.RootElement.TryGetProperty("content", out var contentProp))
                    {
                        byte[] rawBytes = Convert.FromBase64String(contentProp.GetString()?.Replace("\n", "").Replace("\r", "") ?? "");
                        string decoded = Encoding.UTF8.GetString(rawBytes);
                        catalog = JsonSerializer.Deserialize<List<ZenQExtensionItem>>(decoded, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ZenQExtensionItem>();
                    }
                }

                // Cập nhật hoặc thêm mới
                int existingIdx = catalog.FindIndex(x => string.Equals(x.id, meta.id, StringComparison.OrdinalIgnoreCase));
                if (existingIdx >= 0)
                {
                    catalog[existingIdx] = meta;
                }
                else
                {
                    catalog.Add(meta);
                }

                string updatedJson = JsonSerializer.Serialize(catalog, new JsonSerializerOptions { WriteIndented = true });
                var putCatalogObj = new Dictionary<string, object>
                {
                    ["message"] = $"Update extensions_catalog.json with {meta.id}",
                    ["content"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(updatedJson)),
                    ["branch"] = "main"
                };
                if (!string.IsNullOrEmpty(catalogSha)) putCatalogObj["sha"] = catalogSha;

                using var putContent = new StringContent(JsonSerializer.Serialize(putCatalogObj), Encoding.UTF8, "application/json");
                await client.PutAsync(GITHUB_CATALOG_URL, putContent).ConfigureAwait(false);
            }
            catch { }
        }

        private static async Task RemoveFromCloudCatalogJsonAsync(HttpClient client, string extensionId)
        {
            try
            {
                string catalogSha = await GetGitHubFileShaAsync(client, GITHUB_CATALOG_URL).ConfigureAwait(false);
                List<ZenQExtensionItem> catalog = new List<ZenQExtensionItem>();

                var getResp = await client.GetAsync(GITHUB_CATALOG_URL).ConfigureAwait(false);
                if (getResp.IsSuccessStatusCode)
                {
                    string getJson = await getResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using var doc = JsonDocument.Parse(getJson);
                    if (doc.RootElement.TryGetProperty("content", out var contentProp))
                    {
                        byte[] rawBytes = Convert.FromBase64String(contentProp.GetString()?.Replace("\n", "").Replace("\r", "") ?? "");
                        string decoded = Encoding.UTF8.GetString(rawBytes);
                        catalog = JsonSerializer.Deserialize<List<ZenQExtensionItem>>(decoded, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ZenQExtensionItem>();
                    }
                }

                catalog.RemoveAll(x => string.Equals(x.id, extensionId, StringComparison.OrdinalIgnoreCase));

                string updatedJson = JsonSerializer.Serialize(catalog, new JsonSerializerOptions { WriteIndented = true });
                var putCatalogObj = new Dictionary<string, object>
                {
                    ["message"] = $"Remove {extensionId} from extensions_catalog.json",
                    ["content"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(updatedJson)),
                    ["branch"] = "main"
                };
                if (!string.IsNullOrEmpty(catalogSha)) putCatalogObj["sha"] = catalogSha;

                using var putContent = new StringContent(JsonSerializer.Serialize(putCatalogObj), Encoding.UTF8, "application/json");
                await client.PutAsync(GITHUB_CATALOG_URL, putContent).ConfigureAwait(false);
            }
            catch { }
        }

        private static async Task<bool> DeleteGitHubFileIfExistsAsync(HttpClient client, string apiUrl, string commitMessage)
        {
            try
            {
                string sha = await GetGitHubFileShaAsync(client, apiUrl).ConfigureAwait(false);
                if (string.IsNullOrEmpty(sha)) return false;

                var deleteObj = new Dictionary<string, object>
                {
                    ["message"] = commitMessage,
                    ["sha"] = sha,
                    ["branch"] = "main"
                };
                using var request = new HttpRequestMessage(HttpMethod.Delete, apiUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(deleteObj), Encoding.UTF8, "application/json")
                };
                var response = await client.SendAsync(request).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
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

        private static string GetGitHubToken()
        {
            string envToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (!string.IsNullOrWhiteSpace(envToken)) return envToken.Trim();

            string[] targets = new[]
            {
                "git:https://danhtruong6112009-source@github.com",
                "git:https://github.com"
            };

            foreach (var target in targets)
            {
                try
                {
                    if (CredReadW(target, 1, 0, out IntPtr pCred))
                    {
                        try
                        {
                            var cred = Marshal.PtrToStructure<CREDENTIAL>(pCred);
                            if (cred.CredentialBlobSize > 0 && cred.CredentialBlob != IntPtr.Zero)
                            {
                                byte[] raw = new byte[cred.CredentialBlobSize];
                                Marshal.Copy(cred.CredentialBlob, raw, 0, cred.CredentialBlobSize);
                                string tok = Encoding.Unicode.GetString(raw).Trim('\0', ' ', '\r', '\n', '\t');
                                if (tok.StartsWith("ghp_", StringComparison.OrdinalIgnoreCase) || tok.StartsWith("github_pat_", StringComparison.OrdinalIgnoreCase))
                                {
                                    return tok;
                                }
                                tok = Encoding.UTF8.GetString(raw).Trim('\0', ' ', '\r', '\n', '\t');
                                if (tok.StartsWith("ghp_", StringComparison.OrdinalIgnoreCase) || tok.StartsWith("github_pat_", StringComparison.OrdinalIgnoreCase))
                                {
                                    return tok;
                                }
                            }
                        }
                        finally
                        {
                            CredFree(pCred);
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredReadW(string target, int type, int reservedFlag, out IntPtr credentialPtr);

        [DllImport("Advapi32.dll", SetLastError = true)]
        private static extern void CredFree(IntPtr credentialPtr);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public int Flags;
            public int Type;
            public string TargetName;
            public string Comment;
            public long LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }
    }
}

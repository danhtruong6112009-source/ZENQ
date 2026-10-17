using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ZenQOffice.Core
{
    public class BackupItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string OriginalPath { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
        public string BackupFileName { get; set; } = "";
        public string BackupFullPath { get; set; } = "";
        public DateTime BackupTime { get; set; } = DateTime.Now;
        public long FileSizeBytes { get; set; } = 0;
        public string Mode { get; set; } = "Writer";
        public string Sha256 { get; set; } = "";
    }

    public class BackupCatalog
    {
        public List<BackupItem> Items { get; set; } = new List<BackupItem>();
    }

    /// <summary>
    /// Hệ thống Sao lưu File Gốc An toàn (Safety Vault) cho ZenQ Office.
    /// Tự động sao lưu nguyên vẹn tệp gốc khi mở hoặc lưu đè để bảo vệ người dùng,
    /// hỗ trợ xuất/cắt chuyển file backup ra Desktop hoặc thư mục bất kỳ,
    /// đồng thời áp dụng chính sách xoay vòng thông minh (Smart Retention) chống nặng ổ cứng.
    /// </summary>
    public static class BackupManager
    {
        private static readonly object _lock = new object();

        public static string BackupFolder
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenOffice", "backups");
                if (!Directory.Exists(dir))
                {
                    try { Directory.CreateDirectory(dir); } catch { }
                }
                return dir;
            }
        }

        public static string CatalogPath => Path.Combine(BackupFolder, "backup_catalog.json");

        public static List<BackupItem> GetAllBackups()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(CatalogPath))
                    {
                        string json = File.ReadAllText(CatalogPath, Encoding.UTF8);
                        var cat = JsonSerializer.Deserialize<BackupCatalog>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (cat != null && cat.Items != null)
                        {
                            // Filter out items whose physical backup file no longer exists
                            cat.Items.RemoveAll(it => !File.Exists(it.BackupFullPath));
                            return cat.Items;
                        }
                    }
                }
                catch { }
                return new List<BackupItem>();
            }
        }

        public static void SaveCatalog(List<BackupItem> items)
        {
            lock (_lock)
            {
                try
                {
                    var cat = new BackupCatalog { Items = items };
                    string json = JsonSerializer.Serialize(cat, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(CatalogPath, json, Encoding.UTF8);
                }
                catch { }
            }
        }

        public static BackupItem BackupOriginalFile(string originalFilePath, string mode = "Writer")
        {
            if (string.IsNullOrEmpty(originalFilePath) || !File.Exists(originalFilePath))
                return null;

            // Never backup files that are in autosave or backups folders
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (originalFilePath.IndexOf(Path.Combine(appData, "GenOffice", "autosave"), StringComparison.OrdinalIgnoreCase) >= 0 ||
                originalFilePath.IndexOf(Path.Combine(appData, "GenOffice", "backups"), StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return null;
            }

            lock (_lock)
            {
                try
                {
                    var items = GetAllBackups();
                    FileInfo fi = new FileInfo(originalFilePath);
                    string hash = ComputeFileSha256(originalFilePath);

                    // Deduplication check: If this exact file was already backed up and has identical hash, avoid creating duplicate file
                    var existing = items.Find(it => it.OriginalPath.Equals(originalFilePath, StringComparison.OrdinalIgnoreCase) && it.Sha256 == hash);
                    if (existing != null && File.Exists(existing.BackupFullPath))
                    {
                        return existing;
                    }

                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string cleanFileName = Path.GetFileNameWithoutExtension(originalFilePath);
                    string ext = Path.GetExtension(originalFilePath);
                    string backupFileName = $"{cleanFileName}_backup_{timestamp}{ext}";
                    string destPath = Path.Combine(BackupFolder, backupFileName);

                    // Byte-for-byte pristine copy
                    File.Copy(originalFilePath, destPath, true);

                    var newItem = new BackupItem
                    {
                        OriginalPath = originalFilePath,
                        OriginalFileName = Path.GetFileName(originalFilePath),
                        BackupFileName = backupFileName,
                        BackupFullPath = destPath,
                        BackupTime = DateTime.Now,
                        FileSizeBytes = fi.Length,
                        Mode = mode,
                        Sha256 = hash
                    };

                    items.Insert(0, newItem);
                    SaveCatalog(items);

                    // Enforce smart retention policy
                    EnforceRetentionPolicy(items);

                    return newItem;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("BackupManager.BackupOriginalFile error: " + ex.Message);
                    return null;
                }
            }
        }

        /// <summary>
        /// Khôi phục file backup ghi đè lại vị trí gốc ban đầu.
        /// </summary>
        public static bool RestoreToOriginal(BackupItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.BackupFullPath) || !File.Exists(item.BackupFullPath))
                return false;

            try
            {
                string targetDir = Path.GetDirectoryName(item.OriginalPath);
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                File.Copy(item.BackupFullPath, item.OriginalPath, true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("RestoreToOriginal error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Di chuyển hoặc sao chép file backup sang thư mục bất kỳ (ví dụ Desktop).
        /// </summary>
        public static bool RelocateBackup(BackupItem item, string targetFolder, bool cutAndPaste, out string finalPath)
        {
            finalPath = "";
            if (item == null || string.IsNullOrEmpty(item.BackupFullPath) || !File.Exists(item.BackupFullPath))
                return false;

            try
            {
                if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);
                string destFile = Path.Combine(targetFolder, item.OriginalFileName);
                if (File.Exists(destFile))
                {
                    string baseName = Path.GetFileNameWithoutExtension(item.OriginalFileName);
                    string ext = Path.GetExtension(item.OriginalFileName);
                    destFile = Path.Combine(targetFolder, $"{baseName}_phuchoi_{DateTime.Now:yyyyMMdd_HHmm}{ext}");
                }

                if (cutAndPaste)
                {
                    File.Move(item.BackupFullPath, destFile);
                    lock (_lock)
                    {
                        var items = GetAllBackups();
                        items.RemoveAll(x => x.Id == item.Id);
                        SaveCatalog(items);
                    }
                }
                else
                {
                    File.Copy(item.BackupFullPath, destFile, true);
                }

                finalPath = destFile;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("RelocateBackup error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Xóa bỏ một bản sao lưu cụ thể.
        /// </summary>
        public static bool DeleteBackup(BackupItem item)
        {
            if (item == null) return false;
            lock (_lock)
            {
                try
                {
                    if (File.Exists(item.BackupFullPath))
                    {
                        File.Delete(item.BackupFullPath);
                    }
                    var items = GetAllBackups();
                    items.RemoveAll(x => x.Id == item.Id);
                    SaveCatalog(items);
                    return true;
                }
                catch { return false; }
            }
        }

        /// <summary>
        /// Dọn dẹp các bản sao lưu cũ hơn số ngày chỉ định (ví dụ 7 ngày, 30 ngày).
        /// </summary>
        public static int PruneBackupsOlderThan(int days)
        {
            lock (_lock)
            {
                int deletedCount = 0;
                try
                {
                    DateTime cutoff = DateTime.Now.AddDays(-days);
                    var items = GetAllBackups();
                    var toRemove = new List<BackupItem>();

                    // Always preserve at least 1 newest backup per original file
                    var grouped = new Dictionary<string, List<BackupItem>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var it in items)
                    {
                        if (!grouped.ContainsKey(it.OriginalPath))
                            grouped[it.OriginalPath] = new List<BackupItem>();
                        grouped[it.OriginalPath].Add(it);
                    }

                    foreach (var kvp in grouped)
                    {
                        var list = kvp.Value;
                        // Sort descending by time
                        list.Sort((a, b) => b.BackupTime.CompareTo(a.BackupTime));
                        for (int i = 1; i < list.Count; i++) // Keep index 0 (latest)
                        {
                            if (list[i].BackupTime < cutoff)
                            {
                                toRemove.Add(list[i]);
                            }
                        }
                    }

                    foreach (var item in toRemove)
                    {
                        try
                        {
                            if (File.Exists(item.BackupFullPath)) File.Delete(item.BackupFullPath);
                            items.Remove(item);
                            deletedCount++;
                        }
                        catch { }
                    }

                    if (deletedCount > 0)
                    {
                        SaveCatalog(items);
                    }
                }
                catch { }
                return deletedCount;
            }
        }

        /// <summary>
        /// Dọn dẹp toàn bộ kho sao lưu.
        /// </summary>
        public static int ClearAllBackups()
        {
            lock (_lock)
            {
                int count = 0;
                try
                {
                    var items = GetAllBackups();
                    foreach (var it in items)
                    {
                        try
                        {
                            if (File.Exists(it.BackupFullPath)) File.Delete(it.BackupFullPath);
                            count++;
                        }
                        catch { }
                    }
                    SaveCatalog(new List<BackupItem>());
                }
                catch { }
                return count;
            }
        }

        /// <summary>
        /// Tính tổng dung lượng hiện đang chiếm bởi thư mục sao lưu.
        /// </summary>
        public static long GetTotalBackupSizeBytes()
        {
            lock (_lock)
            {
                long total = 0;
                try
                {
                    var items = GetAllBackups();
                    foreach (var it in items)
                    {
                        if (File.Exists(it.BackupFullPath))
                        {
                            try { total += new FileInfo(it.BackupFullPath).Length; } catch { }
                        }
                    }
                }
                catch { }
                return total;
            }
        }

        public static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.0") + " KB";
            if (bytes < 1024 * 1024 * 1024) return (bytes / (1024.0 * 1024.0)).ToString("0.0") + " MB";
            return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("0.0") + " GB";
        }

        /// <summary>
        /// Kích hoạt kiểm tra và thực thi chính sách xoay vòng lưu trữ an toàn (Smart Retention).
        /// </summary>
        public static void EnforceRetentionPolicy()
        {
            lock (_lock)
            {
                var items = GetAllBackups();
                EnforceRetentionPolicy(items);
                SaveCatalog(items);
            }
        }

        /// <summary>
        /// Chính sách xoay vòng tự động (Smart Retention):
        /// 1. Tối đa 5 bản sao lưu cho mỗi tệp gốc.
        /// 2. Tối đa 50 bản sao lưu tổng cộng toàn hệ thống.
        /// 3. Giới hạn tổng dung lượng kho backup không quá 200 MB (tự động xóa bản cũ nhất).
        /// 4. Tự động dọn dẹp các bản sao lưu quá 30 ngày (vẫn giữ lại bản mới nhất của mỗi file).
        /// </summary>
        private static void EnforceRetentionPolicy(List<BackupItem> items)
        {
            try
            {
                bool changed = false;

                // 1. Group by OriginalPath and cap at 5 versions per file
                var grouped = new Dictionary<string, List<BackupItem>>(StringComparer.OrdinalIgnoreCase);
                foreach (var it in items)
                {
                    if (!grouped.ContainsKey(it.OriginalPath))
                        grouped[it.OriginalPath] = new List<BackupItem>();
                    grouped[it.OriginalPath].Add(it);
                }

                foreach (var kvp in grouped)
                {
                    var list = kvp.Value;
                    list.Sort((a, b) => b.BackupTime.CompareTo(a.BackupTime));
                    if (list.Count > 5)
                    {
                        for (int i = 5; i < list.Count; i++)
                        {
                            try
                            {
                                if (File.Exists(list[i].BackupFullPath)) File.Delete(list[i].BackupFullPath);
                                items.Remove(list[i]);
                                changed = true;
                            }
                            catch { }
                        }
                    }
                }

                // 2. Cap total items at 50
                if (items.Count > 50)
                {
                    items.Sort((a, b) => b.BackupTime.CompareTo(a.BackupTime));
                    while (items.Count > 50)
                    {
                        var last = items[items.Count - 1];
                        try
                        {
                            if (File.Exists(last.BackupFullPath)) File.Delete(last.BackupFullPath);
                        }
                        catch { }
                        items.RemoveAt(items.Count - 1);
                        changed = true;
                    }
                }

                // 3. Cap total storage at 200 MB
                long totalSize = 0;
                foreach (var it in items) totalSize += it.FileSizeBytes;
                long maxSizeBytes = 200L * 1024 * 1024; // 200 MB

                if (totalSize > maxSizeBytes)
                {
                    items.Sort((a, b) => b.BackupTime.CompareTo(a.BackupTime));
                    for (int i = items.Count - 1; i >= 0 && totalSize > maxSizeBytes; i--)
                    {
                        var it = items[i];
                        try
                        {
                            if (File.Exists(it.BackupFullPath)) File.Delete(it.BackupFullPath);
                            totalSize -= it.FileSizeBytes;
                            items.RemoveAt(i);
                            changed = true;
                        }
                        catch { }
                    }
                }

                if (changed)
                {
                    SaveCatalog(items);
                }
            }
            catch { }
        }

        private static string ComputeFileSha256(string filePath)
        {
            try
            {
                using (var sha = SHA256.Create())
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] hashBytes = sha.ComputeHash(fs);
                    StringBuilder sb = new StringBuilder(hashBytes.Length * 2);
                    foreach (byte b in hashBytes) sb.Append(b.ToString("x2"));
                    return sb.ToString();
                }
            }
            catch { return ""; }
        }
    }
}

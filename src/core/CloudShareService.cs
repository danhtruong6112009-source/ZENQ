using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using ZenQOffice.UI;

namespace ZenQOffice.Core
{
    public class CloudShareItem
    {
        public Guid Id { get; set; }
        public int SenderId { get; set; }
        public string SenderUsername { get; set; } = "";
        public string SenderName { get; set; } = "";
        public int RecipientId { get; set; }
        public string RecipientUsername { get; set; } = "";
        public string RecipientName { get; set; } = "";
        public string FileName { get; set; } = "";
        public int DocMode { get; set; } = (int)OfficeAppMode.Writer;
        public long FileSize { get; set; }
        public string Sha256 { get; set; } = "";
        public string Note { get; set; } = "";
        public int ChunkCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? DownloadedAt { get; set; }
        public string LocalFilePath { get; set; } = "";
        public bool IsDownloadedLocally => !string.IsNullOrEmpty(LocalFilePath) && File.Exists(LocalFilePath);
        public int RemainingDays => Math.Max(0, (int)Math.Ceiling((ExpiresAt - DateTime.UtcNow).TotalDays));
    }

    public class RegisteredRecipientUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "user";

        public string DisplayLabel =>
            string.IsNullOrWhiteSpace(FullName)
                ? $"@{Username} ({Email})"
                : $"{FullName} (@{Username})";
    }

    public class CloudShareOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public CloudShareItem Item { get; set; }
    }

    /// <summary>
    /// Dịch vụ chia sẻ tệp qua CSDL PostgreSQL với định mức ổ đĩa Cloud 200 MB/người gửi,
    /// tự động tải về trên máy người nhận và lưu giữ trên máy chủ tối đa 30 ngày.
    /// </summary>
    public static class CloudShareService
    {
        public const long CloudQuotaBytes = 200L * 1024L * 1024L; // 200 MB per sender
        public const int RetentionDays = 30;
        public const int ChunkSizeBytes = 2 * 1024 * 1024; // 2 MB chunk size

        private static bool _initialized = false;
        private static bool _schemaReady = false;
        private static readonly SemaphoreSlim _syncGate = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim _schemaGate = new SemaphoreSlim(1, 1);
        private static readonly object _manifestLock = new object();

        /// <summary>Optional test override to isolate local manifest & downloaded files.</summary>
        public static string LocalStorageOverrideDir { get; set; }

        public static List<CloudShareItem> InboxItems { get; private set; } = new List<CloudShareItem>();
        public static List<CloudShareItem> SentItems { get; private set; } = new List<CloudShareItem>();
        public static long SenderUsedBytes { get; private set; } = 0;
        public static bool IsRefreshing { get; private set; } = false;
        public static string LastSyncError { get; private set; } = "";
        public static ConcurrentDictionary<Guid, int> DownloadProgress { get; } = new ConcurrentDictionary<Guid, int>();

        public static event Action OnShareStateUpdated;
        public static event Action<CloudShareItem, string> OnShareAutoDownloaded;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            DatabaseAuthService.OnAuthStateChanged += HandleAuthStateChanged;

            Task.Run(async () =>
            {
                try
                {
                    await EnsureTablesCreatedAsync().ConfigureAwait(false);
                    if (DatabaseAuthService.IsLoggedIn)
                    {
                        await RefreshAndAutoDownloadAsync().ConfigureAwait(false);
                    }
                }
                catch { }
            });
        }

        private static void HandleAuthStateChanged()
        {
            if (!DatabaseAuthService.IsLoggedIn)
            {
                InboxItems = new List<CloudShareItem>();
                SentItems = new List<CloudShareItem>();
                SenderUsedBytes = 0;
                LastSyncError = "";
                DownloadProgress.Clear();
                OnShareStateUpdated?.Invoke();
                return;
            }

            Task.Run(async () =>
            {
                try { await RefreshAndAutoDownloadAsync().ConfigureAwait(false); } catch { }
            });
        }

        public static string GetReceivedFilesDir()
        {
            string dir;
            if (!string.IsNullOrEmpty(LocalStorageOverrideDir))
            {
                dir = Path.Combine(LocalStorageOverrideDir, "Received");
            }
            else
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (string.IsNullOrEmpty(docs))
                    docs = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                dir = Path.Combine(docs, "ZenQOffice Shared");
            }
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        private static string GetManifestPath()
        {
            string baseDir;
            if (!string.IsNullOrEmpty(LocalStorageOverrideDir))
            {
                baseDir = LocalStorageOverrideDir;
            }
            else
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                baseDir = Path.Combine(appData, "GenOffice");
            }
            if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);
            return Path.Combine(baseDir, "shared_received.json");
        }

        private static Dictionary<string, string> LoadReceivedManifest()
        {
            lock (_manifestLock)
            {
                try
                {
                    string path = GetManifestPath();
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path, Encoding.UTF8);
                        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                        if (dict != null) return dict;
                    }
                }
                catch { }
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static void SaveReceivedManifestEntry(Guid shareId, string localPath)
        {
            lock (_manifestLock)
            {
                try
                {
                    var dict = LoadReceivedManifest();
                    dict[shareId.ToString("N")] = localPath;
                    string path = GetManifestPath();
                    string json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                    string tmp = path + ".tmp";
                    File.WriteAllText(tmp, json, new UTF8Encoding(false));
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(tmp, path);
                }
                catch { }
            }
        }

        public static void RemoveReceivedManifestEntry(Guid shareId)
        {
            lock (_manifestLock)
            {
                try
                {
                    var dict = LoadReceivedManifest();
                    if (dict.Remove(shareId.ToString("N")))
                    {
                        string path = GetManifestPath();
                        string json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(path, json, new UTF8Encoding(false));
                    }
                }
                catch { }
            }
        }

        public static async Task EnsureTablesCreatedAsync()
        {
            if (_schemaReady) return;
            await _schemaGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_schemaReady) return;
                await DatabaseAuthService.EnsureTablesCreatedAsync().ConfigureAwait(false);
                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                string ddl = @"
                CREATE TABLE IF NOT EXISTS cloud_share_items (
                    id UUID PRIMARY KEY,
                    sender_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                    sender_username VARCHAR(50) NOT NULL,
                    sender_name VARCHAR(120),
                    recipient_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                    recipient_username VARCHAR(50) NOT NULL,
                    recipient_name VARCHAR(120),
                    file_name VARCHAR(255) NOT NULL,
                    doc_mode INT NOT NULL DEFAULT 1,
                    file_size BIGINT NOT NULL,
                    sha256 VARCHAR(64) NOT NULL,
                    note TEXT,
                    chunk_count INT NOT NULL,
                    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
                    downloaded_at TIMESTAMP WITH TIME ZONE,
                    deleted_by_sender BOOLEAN NOT NULL DEFAULT FALSE
                );
                CREATE INDEX IF NOT EXISTS idx_cloud_share_recipient ON cloud_share_items(recipient_id, created_at DESC);
                CREATE INDEX IF NOT EXISTS idx_cloud_share_sender ON cloud_share_items(sender_id, created_at DESC);
                CREATE INDEX IF NOT EXISTS idx_cloud_share_expires ON cloud_share_items(expires_at);

                CREATE TABLE IF NOT EXISTS cloud_share_chunks (
                    share_id UUID NOT NULL REFERENCES cloud_share_items(id) ON DELETE CASCADE,
                    chunk_index INT NOT NULL,
                    chunk_data BYTEA NOT NULL,
                    PRIMARY KEY (share_id, chunk_index)
                );
                ";
                using var cmd = new NpgsqlCommand(ddl, conn);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                _schemaReady = true;
            }
            catch (Exception ex)
            {
                LastSyncError = ex.Message;
            }
            finally
            {
                _schemaGate.Release();
            }
        }

        public static async Task PurgeExpiredSharesAsync(NpgsqlConnection existingConn = null)
        {
            try
            {
                await EnsureTablesCreatedAsync().ConfigureAwait(false);
                bool ownsConn = existingConn == null;
                NpgsqlConnection conn = existingConn ?? new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                try
                {
                    if (ownsConn) await conn.OpenAsync().ConfigureAwait(false);
                    string sql = "DELETE FROM cloud_share_items WHERE expires_at <= CURRENT_TIMESTAMP OR deleted_by_sender = TRUE;";
                    using var cmd = new NpgsqlCommand(sql, conn);
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                finally
                {
                    if (ownsConn) conn.Dispose();
                }
            }
            catch { }
        }

        public static async Task<List<RegisteredRecipientUser>> SearchRegisteredUsersAsync(string query, int limit = 8)
        {
            var results = new List<RegisteredRecipientUser>();
            try
            {
                await EnsureTablesCreatedAsync().ConfigureAwait(false);
                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                string q = (query ?? "").Trim().TrimStart('@');
                int curId = DatabaseAuthService.CurrentUser?.Id ?? 0;

                string sql;
                if (string.IsNullOrEmpty(q))
                {
                    sql = @"SELECT id, username, email, COALESCE(full_name, '') as full_name, COALESCE(role, 'user') as role
                            FROM users
                            WHERE COALESCE(ban, FALSE) = FALSE AND id <> @curId
                            ORDER BY last_login DESC NULLS LAST, id ASC
                            LIMIT @lim;";
                }
                else
                {
                    sql = @"SELECT id, username, email, COALESCE(full_name, '') as full_name, COALESCE(role, 'user') as role
                            FROM users
                            WHERE COALESCE(ban, FALSE) = FALSE
                              AND (LOWER(username) LIKE @pat OR LOWER(email) LIKE @pat OR LOWER(COALESCE(full_name,'')) LIKE @pat)
                            ORDER BY CASE WHEN LOWER(username) = LOWER(@exact) OR LOWER(email) = LOWER(@exact) THEN 0 ELSE 1 END, username ASC
                            LIMIT @lim;";
                }

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("curId", curId);
                cmd.Parameters.AddWithValue("lim", Math.Max(1, limit));
                if (!string.IsNullOrEmpty(q))
                {
                    cmd.Parameters.AddWithValue("pat", "%" + q.ToLowerInvariant() + "%");
                    cmd.Parameters.AddWithValue("exact", q);
                }

                using var r = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await r.ReadAsync().ConfigureAwait(false))
                {
                    results.Add(new RegisteredRecipientUser
                    {
                        Id = r.GetInt32(0),
                        Username = r.GetString(1),
                        Email = r.GetString(2),
                        FullName = r.IsDBNull(3) ? "" : r.GetString(3),
                        Role = r.IsDBNull(4) ? "user" : r.GetString(4)
                    });
                }
            }
            catch { }
            return results;
        }

        public static async Task<RegisteredRecipientUser> FindRegisteredUserAsync(string usernameOrEmail)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail)) return null;
            await EnsureTablesCreatedAsync().ConfigureAwait(false);
            string q = usernameOrEmail.Trim().TrimStart('@');

            using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
            await conn.OpenAsync().ConfigureAwait(false);

            string sql = @"SELECT id, username, email, COALESCE(full_name, '') as full_name, COALESCE(role, 'user') as role
                           FROM users
                           WHERE COALESCE(ban, FALSE) = FALSE
                             AND (LOWER(username) = LOWER(@q) OR LOWER(email) = LOWER(@q))
                           LIMIT 1;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("q", q);
            using var r = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (await r.ReadAsync().ConfigureAwait(false))
            {
                return new RegisteredRecipientUser
                {
                    Id = r.GetInt32(0),
                    Username = r.GetString(1),
                    Email = r.GetString(2),
                    FullName = r.IsDBNull(3) ? "" : r.GetString(3),
                    Role = r.IsDBNull(4) ? "user" : r.GetString(4)
                };
            }
            return null;
        }

        public static async Task<long> GetSenderUsedBytesAsync(int senderId)
        {
            await EnsureTablesCreatedAsync().ConfigureAwait(false);
            using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
            await conn.OpenAsync().ConfigureAwait(false);
            await PurgeExpiredSharesAsync(conn).ConfigureAwait(false);

            string sql = @"SELECT COALESCE(SUM(file_size), 0)::BIGINT
                           FROM cloud_share_items
                           WHERE sender_id = @sid AND deleted_by_sender = FALSE AND expires_at > CURRENT_TIMESTAMP;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("sid", senderId);
            object res = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return (res != null && res != DBNull.Value) ? Convert.ToInt64(res) : 0L;
        }

        public static async Task<CloudShareOperationResult> SendFileAsync(
            string recipientInput,
            string filePath,
            OfficeAppMode docMode,
            string note = "",
            IProgress<(int percent, string status)> progress = null)
        {
            var sender = DatabaseAuthService.CurrentUser;
            if (!DatabaseAuthService.IsLoggedIn || sender == null)
            {
                return new CloudShareOperationResult
                {
                    Success = false,
                    Message = AppL10n.T("Bạn cần đăng nhập tài khoản ZenQ để chia sẻ tệp.", "You must sign in to a ZenQ account to share files.")
                };
            }

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return new CloudShareOperationResult
                {
                    Success = false,
                    Message = AppL10n.T("Không tìm thấy tệp cần gửi.", "Source file could not be found.")
                };
            }

            try
            {
                await DatabaseAuthService.RefreshCurrentUserAsync().ConfigureAwait(false);
                sender = DatabaseAuthService.CurrentUser;
                if (sender == null || sender.Ban)
                {
                    return new CloudShareOperationResult
                    {
                        Success = false,
                        Message = AppL10n.T("Tài khoản của bạn chưa hợp lệ hoặc đang bị khóa.", "Your account is invalid or suspended.")
                    };
                }

                await EnsureTablesCreatedAsync().ConfigureAwait(false);

                progress?.Report((3, AppL10n.T("Đang kiểm tra tài khoản người nhận...", "Verifying recipient account...")));
                var recipient = await FindRegisteredUserAsync(recipientInput).ConfigureAwait(false);
                if (recipient == null)
                {
                    return new CloudShareOperationResult
                    {
                        Success = false,
                        Message = AppL10n.T("Không tìm thấy tài khoản người nhận. Hãy kiểm tra lại tên đăng nhập hoặc email.", "Recipient account not found. Please check the username or email.")
                    };
                }

                var fi = new FileInfo(filePath);
                long fileSize = fi.Length;
                if (fileSize <= 0)
                {
                    return new CloudShareOperationResult
                    {
                        Success = false,
                        Message = AppL10n.T("Tệp rỗng (0 byte), không thể gửi.", "Cannot send an empty (0-byte) file.")
                    };
                }

                if (fileSize > CloudQuotaBytes)
                {
                    return new CloudShareOperationResult
                    {
                        Success = false,
                        Message = string.Format(
                            AppL10n.T("Tệp có dung lượng {0}, vượt quá giới hạn ổ đĩa Cloud chia sẻ 200 MB.", "File size is {0}, exceeding the 200 MB Cloud Share quota."),
                            FormatSize(fileSize))
                    };
                }

                progress?.Report((8, AppL10n.T("Đang chuẩn bị tệp gửi...", "Preparing file...")));
                string shaHex;
                using (var fsHash = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(fsHash);
                    shaHex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }

                int chunkCount = (int)Math.Ceiling((double)fileSize / ChunkSizeBytes);
                Guid shareId = Guid.NewGuid();
                string fileName = Path.GetFileName(filePath);
                if (string.IsNullOrEmpty(fileName)) fileName = "Document.docx";
                string senderDisplay = !string.IsNullOrWhiteSpace(sender.FullName) ? sender.FullName : sender.Username;
                string recipientDisplay = !string.IsNullOrWhiteSpace(recipient.FullName) ? recipient.FullName : recipient.Username;
                DateTime createdAt = DateTime.UtcNow;
                DateTime expiresAt = createdAt.AddDays(RetentionDays);

                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);
                await PurgeExpiredSharesAsync(conn).ConfigureAwait(false);

                using var tx = await conn.BeginTransactionAsync().ConfigureAwait(false);
                try
                {
                    // Serialize per-sender quota checks across concurrent clients
                    using (var lockCmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@sid);", conn, tx))
                    {
                        lockCmd.Parameters.AddWithValue("sid", (long)sender.Id);
                        await lockCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }

                    long currentUsed = 0;
                    using (var quotaCmd = new NpgsqlCommand(
                        "SELECT COALESCE(SUM(file_size), 0)::BIGINT FROM cloud_share_items WHERE sender_id = @sid AND deleted_by_sender = FALSE AND expires_at > CURRENT_TIMESTAMP;",
                        conn, tx))
                    {
                        quotaCmd.Parameters.AddWithValue("sid", sender.Id);
                        object qRes = await quotaCmd.ExecuteScalarAsync().ConfigureAwait(false);
                        currentUsed = (qRes != null && qRes != DBNull.Value) ? Convert.ToInt64(qRes) : 0L;
                    }

                    if (currentUsed + fileSize > CloudQuotaBytes)
                    {
                        await tx.RollbackAsync().ConfigureAwait(false);
                        SenderUsedBytes = currentUsed;
                        OnShareStateUpdated?.Invoke();
                        long remain = Math.Max(0, CloudQuotaBytes - currentUsed);
                        return new CloudShareOperationResult
                        {
                            Success = false,
                            Message = string.Format(
                                AppL10n.T(
                                    "Ổ đĩa Cloud chia sẻ của bạn không đủ dung lượng!\nĐã dùng: {0} / 200 MB (còn trống {1}). Tệp này nặng {2}.\nHãy xóa bớt tệp đã gửi hoặc chờ tự động dọn sau 30 ngày.",
                                    "Insufficient Cloud Share storage!\nUsed: {0} / 200 MB ({1} free). This file is {2}.\nDelete an old shared file or wait for the 30-day auto cleanup."),
                                FormatSize(currentUsed), FormatSize(remain), FormatSize(fileSize))
                        };
                    }

                    string insertItemSql = @"
                        INSERT INTO cloud_share_items
                        (id, sender_id, sender_username, sender_name, recipient_id, recipient_username, recipient_name,
                         file_name, doc_mode, file_size, sha256, note, chunk_count, created_at, expires_at, deleted_by_sender)
                        VALUES
                        (@id, @sid, @sun, @snm, @rid, @run, @rnm,
                         @fn, @dm, @fsz, @sha, @note, @cc, @ca, @ea, FALSE);";
                    using (var insCmd = new NpgsqlCommand(insertItemSql, conn, tx))
                    {
                        insCmd.Parameters.AddWithValue("id", shareId);
                        insCmd.Parameters.AddWithValue("sid", sender.Id);
                        insCmd.Parameters.AddWithValue("sun", sender.Username ?? "");
                        insCmd.Parameters.AddWithValue("snm", senderDisplay ?? "");
                        insCmd.Parameters.AddWithValue("rid", recipient.Id);
                        insCmd.Parameters.AddWithValue("run", recipient.Username ?? "");
                        insCmd.Parameters.AddWithValue("rnm", recipientDisplay ?? "");
                        insCmd.Parameters.AddWithValue("fn", fileName);
                        insCmd.Parameters.AddWithValue("dm", (int)docMode);
                        insCmd.Parameters.AddWithValue("fsz", fileSize);
                        insCmd.Parameters.AddWithValue("sha", shaHex);
                        insCmd.Parameters.AddWithValue("note", note ?? "");
                        insCmd.Parameters.AddWithValue("cc", chunkCount);
                        insCmd.Parameters.AddWithValue("ca", createdAt);
                        insCmd.Parameters.AddWithValue("ea", expiresAt);
                        await insCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }

                    byte[] buffer = new byte[ChunkSizeBytes];
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        for (int idx = 0; idx < chunkCount; idx++)
                        {
                            int totalRead = 0;
                            int targetLen = (int)Math.Min(ChunkSizeBytes, fileSize - (long)idx * ChunkSizeBytes);
                            while (totalRead < targetLen)
                            {
                                int read = await fs.ReadAsync(buffer, totalRead, targetLen - totalRead).ConfigureAwait(false);
                                if (read <= 0) break;
                                totalRead += read;
                            }

                            byte[] chunkPayload;
                            if (totalRead == buffer.Length)
                            {
                                chunkPayload = buffer;
                            }
                            else
                            {
                                chunkPayload = new byte[totalRead];
                                Buffer.BlockCopy(buffer, 0, chunkPayload, 0, totalRead);
                            }

                            using var chkCmd = new NpgsqlCommand(
                                "INSERT INTO cloud_share_chunks (share_id, chunk_index, chunk_data) VALUES (@sid, @idx, @data);",
                                conn, tx);
                            chkCmd.CommandTimeout = 120;
                            chkCmd.Parameters.AddWithValue("sid", shareId);
                            chkCmd.Parameters.AddWithValue("idx", idx);
                            var pData = chkCmd.Parameters.Add("data", NpgsqlDbType.Bytea);
                            pData.Value = chunkPayload;
                            await chkCmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                            int pct = 12 + (int)Math.Round(85.0 * (idx + 1) / Math.Max(1, chunkCount));
                            progress?.Report((Math.Min(98, pct), string.Format(
                                AppL10n.T("Đang gửi tệp ({0}%)...", "Sending ({0}%)..."),
                                Math.Min(98, pct))));
                        }
                    }

                    await tx.CommitAsync().ConfigureAwait(false);
                }
                catch
                {
                    try { await tx.RollbackAsync().ConfigureAwait(false); } catch { }
                    throw;
                }

                progress?.Report((100, AppL10n.T("Gửi thành công!", "Sent successfully!")));
                var createdItem = new CloudShareItem
                {
                    Id = shareId,
                    SenderId = sender.Id,
                    SenderUsername = sender.Username ?? "",
                    SenderName = senderDisplay ?? "",
                    RecipientId = recipient.Id,
                    RecipientUsername = recipient.Username ?? "",
                    RecipientName = recipientDisplay ?? "",
                    FileName = fileName,
                    DocMode = (int)docMode,
                    FileSize = fileSize,
                    Sha256 = shaHex,
                    Note = note ?? "",
                    ChunkCount = chunkCount,
                    CreatedAt = createdAt,
                    ExpiresAt = expiresAt
                };

                await RefreshAndAutoDownloadAsync().ConfigureAwait(false);

                return new CloudShareOperationResult
                {
                    Success = true,
                    Item = createdItem,
                    Message = string.Format(
                        AppL10n.T(
                            "Đã gửi \"{0}\" ({1}) tới @{2}.\nTệp được lưu giữ trên Cloud 30 ngày và sẽ tự động tải về máy người nhận.",
                            "Sent \"{0}\" ({1}) to @{2}.\nRetained on Cloud for 30 days and auto-downloaded to the recipient's PC."),
                        fileName, FormatSize(fileSize), recipient.Username)
                };
            }
            catch (Exception ex)
            {
                return new CloudShareOperationResult
                {
                    Success = false,
                    Message = AppL10n.T("Không thể gửi tệp: ", "Share error: ") + ex.Message
                };
            }
        }

        public static async Task RefreshAndAutoDownloadAsync()
        {
            var cur = DatabaseAuthService.CurrentUser;
            if (!DatabaseAuthService.IsLoggedIn || cur == null) return;

            if (!await _syncGate.WaitAsync(0).ConfigureAwait(false))
                return;

            IsRefreshing = true;
            LastSyncError = "";
            OnShareStateUpdated?.Invoke();

            try
            {
                await EnsureTablesCreatedAsync().ConfigureAwait(false);
                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);
                await PurgeExpiredSharesAsync(conn).ConfigureAwait(false);

                var manifest = LoadReceivedManifest();
                var inbox = new List<CloudShareItem>();
                var sent = new List<CloudShareItem>();
                long usedBytes = 0;

                string inboxSql = @"
                    SELECT id, sender_id, sender_username, COALESCE(sender_name,''), recipient_id, recipient_username, COALESCE(recipient_name,''),
                           file_name, doc_mode, file_size, sha256, COALESCE(note,''), chunk_count, created_at, expires_at, downloaded_at
                    FROM cloud_share_items
                    WHERE recipient_id = @uid AND deleted_by_sender = FALSE AND expires_at > CURRENT_TIMESTAMP
                    ORDER BY created_at DESC;";
                using (var cmd = new NpgsqlCommand(inboxSql, conn))
                {
                    cmd.Parameters.AddWithValue("uid", cur.Id);
                    using var r = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                    while (await r.ReadAsync().ConfigureAwait(false))
                    {
                        var item = ReadShareItem(r);
                        if (manifest.TryGetValue(item.Id.ToString("N"), out string savedPath) && File.Exists(savedPath))
                        {
                            item.LocalFilePath = savedPath;
                        }
                        inbox.Add(item);
                    }
                }

                string sentSql = @"
                    SELECT id, sender_id, sender_username, COALESCE(sender_name,''), recipient_id, recipient_username, COALESCE(recipient_name,''),
                           file_name, doc_mode, file_size, sha256, COALESCE(note,''), chunk_count, created_at, expires_at, downloaded_at
                    FROM cloud_share_items
                    WHERE sender_id = @uid AND deleted_by_sender = FALSE AND expires_at > CURRENT_TIMESTAMP
                    ORDER BY created_at DESC;";
                using (var cmd = new NpgsqlCommand(sentSql, conn))
                {
                    cmd.Parameters.AddWithValue("uid", cur.Id);
                    using var r = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                    while (await r.ReadAsync().ConfigureAwait(false))
                    {
                        var item = ReadShareItem(r);
                        sent.Add(item);
                        usedBytes += item.FileSize;
                    }
                }

                InboxItems = inbox;
                SentItems = sent;
                SenderUsedBytes = usedBytes;
                OnShareStateUpdated?.Invoke();

                // Zalo-style background auto-download for any incoming file not yet stored locally
                // Note: DB row is NOT deleted after download; it stays on Cloud for 30 days as requested.
                foreach (var item in inbox)
                {
                    if (!item.IsDownloadedLocally)
                    {
                        try
                        {
                            string downloadedPath = await DownloadShareInternalAsync(item, conn).ConfigureAwait(false);
                            if (!string.IsNullOrEmpty(downloadedPath) && File.Exists(downloadedPath))
                            {
                                item.LocalFilePath = downloadedPath;
                                if (!item.DownloadedAt.HasValue) item.DownloadedAt = DateTime.UtcNow;
                                OnShareAutoDownloaded?.Invoke(item, downloadedPath);
                                OnShareStateUpdated?.Invoke();
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                LastSyncError = ex.Message;
            }
            finally
            {
                IsRefreshing = false;
                _syncGate.Release();
                OnShareStateUpdated?.Invoke();
            }
        }

        public static async Task<string> ManualDownloadShareAsync(CloudShareItem item)
        {
            if (item == null) return null;
            if (item.IsDownloadedLocally) return item.LocalFilePath;

            await EnsureTablesCreatedAsync().ConfigureAwait(false);
            using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
            await conn.OpenAsync().ConfigureAwait(false);
            string path = await DownloadShareInternalAsync(item, conn).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(path))
            {
                item.LocalFilePath = path;
                if (!item.DownloadedAt.HasValue) item.DownloadedAt = DateTime.UtcNow;
                OnShareStateUpdated?.Invoke();
            }
            return path;
        }

        private static async Task<string> DownloadShareInternalAsync(CloudShareItem item, NpgsqlConnection conn)
        {
            if (item == null) return null;

            string receiveDir = GetReceivedFilesDir();
            string safeFileName = SanitizeFileName(item.FileName);
            string targetPath = GetUniqueTargetPath(receiveDir, safeFileName);
            string tempPartPath = Path.Combine(receiveDir, ".dl_" + item.Id.ToString("N") + ".part");

            DownloadProgress[item.Id] = 1;
            OnShareStateUpdated?.Invoke();

            try
            {
                using (var fs = new FileStream(tempPartPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    string sql = "SELECT chunk_index, chunk_data FROM cloud_share_chunks WHERE share_id = @sid ORDER BY chunk_index ASC;";
                    using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.CommandTimeout = 180;
                    cmd.Parameters.AddWithValue("sid", item.Id);

                    using var r = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess).ConfigureAwait(false);
                    int receivedChunks = 0;
                    while (await r.ReadAsync().ConfigureAwait(false))
                    {
                        byte[] bytes = r.GetFieldValue<byte[]>(1);
                        await fs.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                        receivedChunks++;
                        int pct = Math.Min(98, (int)Math.Round(100.0 * receivedChunks / Math.Max(1, item.ChunkCount)));
                        DownloadProgress[item.Id] = pct;
                        OnShareStateUpdated?.Invoke();
                    }
                }

                // Verify integrity with SHA-256
                if (!string.IsNullOrEmpty(item.Sha256))
                {
                    using var fsVerify = new FileStream(tempPartPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var sha = SHA256.Create();
                    string gotHash = BitConverter.ToString(sha.ComputeHash(fsVerify)).Replace("-", "").ToLowerInvariant();
                    if (!string.Equals(gotHash, item.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(tempPartPath); } catch { }
                        throw new InvalidDataException("Downloaded file checksum mismatch.");
                    }
                }

                if (File.Exists(targetPath)) File.Delete(targetPath);
                File.Move(tempPartPath, targetPath);
                SaveReceivedManifestEntry(item.Id, targetPath);

                // Record downloaded_at on DB without deleting chunks (kept for 30-day retention)
                try
                {
                    using var markCmd = new NpgsqlCommand(
                        "UPDATE cloud_share_items SET downloaded_at = COALESCE(downloaded_at, CURRENT_TIMESTAMP) WHERE id = @id;",
                        conn);
                    markCmd.Parameters.AddWithValue("id", item.Id);
                    await markCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                catch { }

                return targetPath;
            }
            finally
            {
                DownloadProgress.TryRemove(item.Id, out _);
                try { if (File.Exists(tempPartPath)) File.Delete(tempPartPath); } catch { }
                OnShareStateUpdated?.Invoke();
            }
        }

        public static async Task<bool> DeleteSentShareAsync(Guid shareId)
        {
            var cur = DatabaseAuthService.CurrentUser;
            if (!DatabaseAuthService.IsLoggedIn || cur == null) return false;

            try
            {
                await EnsureTablesCreatedAsync().ConfigureAwait(false);
                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                string sql = "DELETE FROM cloud_share_items WHERE id = @id AND sender_id = @sid;";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", shareId);
                cmd.Parameters.AddWithValue("sid", cur.Id);
                int affected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                await RefreshAndAutoDownloadAsync().ConfigureAwait(false);
                return affected > 0;
            }
            catch
            {
                return false;
            }
        }

        private static CloudShareItem ReadShareItem(NpgsqlDataReader r)
        {
            return new CloudShareItem
            {
                Id = r.GetGuid(0),
                SenderId = r.GetInt32(1),
                SenderUsername = r.GetString(2),
                SenderName = r.GetString(3),
                RecipientId = r.GetInt32(4),
                RecipientUsername = r.GetString(5),
                RecipientName = r.GetString(6),
                FileName = r.GetString(7),
                DocMode = r.GetInt32(8),
                FileSize = r.GetInt64(9),
                Sha256 = r.GetString(10),
                Note = r.GetString(11),
                ChunkCount = r.GetInt32(12),
                CreatedAt = r.GetDateTime(13).ToLocalTime(),
                ExpiresAt = r.GetDateTime(14).ToUniversalTime(),
                DownloadedAt = r.IsDBNull(15) ? (DateTime?)null : r.GetDateTime(15).ToLocalTime()
            };
        }

        public static OfficeAppMode ResolveDocMode(string fileName, int hintMode = -1)
        {
            string ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
            switch (ext)
            {
                case ".docx":
                case ".doc":
                case ".rtf":
                case ".odt":
                case ".txt":
                case ".md":
                case ".html":
                case ".htm":
                    return OfficeAppMode.Writer;
                case ".xlsx":
                case ".xls":
                case ".csv":
                    return OfficeAppMode.Calc;
                case ".pptx":
                case ".ppt":
                    return OfficeAppMode.Slides;
                case ".pdf":
                    return OfficeAppMode.Pdf;
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".bmp":
                case ".psd":
                case ".svg":
                case ".tiff":
                    return OfficeAppMode.Photo;
                default:
                    if (hintMode >= (int)OfficeAppMode.Writer && hintMode <= (int)OfficeAppMode.Photo)
                        return (OfficeAppMode)hintMode;
                    return OfficeAppMode.Writer;
            }
        }

        public static string FormatSize(long bytes)
        {
            if (bytes < 1024L) return bytes + " B";
            double kb = bytes / 1024.0;
            if (kb < 1024.0) return kb.ToString("0.#") + " KB";
            double mb = kb / 1024.0;
            return mb.ToString("0.##") + " MB";
        }

        private static string SanitizeFileName(string name)
        {
            string baseName = string.IsNullOrWhiteSpace(name) ? "Document.docx" : Path.GetFileName(name.Trim());
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                baseName = baseName.Replace(c, '_');
            }
            return string.IsNullOrWhiteSpace(baseName) ? "Document.docx" : baseName;
        }

        private static string GetUniqueTargetPath(string dir, string fileName)
        {
            string candidate = Path.Combine(dir, fileName);
            if (!File.Exists(candidate)) return candidate;

            string stem = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            for (int i = 1; i < 10000; i++)
            {
                string next = Path.Combine(dir, $"{stem} ({i}){ext}");
                if (!File.Exists(next)) return next;
            }
            return Path.Combine(dir, $"{stem}_{Guid.NewGuid():N}{ext}");
        }
    }
}

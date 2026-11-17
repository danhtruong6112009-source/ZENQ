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
    public class CollabMemberInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "editor"; // "owner" | "editor"
        public string ColorHex { get; set; } = "#2563EB";
        public DateTime JoinedAt { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? $"@{Username}" : FullName;
    }

    public class CollabPresenceInfo
    {
        public Guid RoomId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string ColorHex { get; set; } = "#2563EB";
        public int SlideIndex { get; set; }
        public string SelectedId { get; set; } = "";
        public float CursorX { get; set; }
        public float CursorY { get; set; }
        public bool IsEditing { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsStale => (DateTime.UtcNow - LastSeen).TotalSeconds > 15;
    }

    public class CollabRoomInfo
    {
        public Guid Id { get; set; }
        public string RoomCode { get; set; } = "";
        public string Title { get; set; } = "";
        public int DocMode { get; set; } = (int)OfficeAppMode.Slides;
        public int OwnerId { get; set; }
        public string OwnerUsername { get; set; } = "";
        public long SnapshotVersion { get; set; } = 1;
        public string SnapshotSha256 { get; set; } = "";
        public long SnapshotSize { get; set; }
        public string UpdatedByUsername { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string MyRole { get; set; } = "editor";
        public List<CollabMemberInfo> Members { get; set; } = new List<CollabMemberInfo>();
        public int OnlineCount { get; set; }
    }

    public class CollabOpRecord
    {
        public long Seq { get; set; }
        public Guid RoomId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public Guid ClientOpId { get; set; }
        public int DocMode { get; set; }
        public string OpType { get; set; } = "";
        public string TargetKey { get; set; } = "";
        public string PayloadJson { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class CollabLiveSignal
    {
        public string Type { get; set; } = ""; // "live_transform", "presence", "op_ping", "snapshot_ping"
        public Guid RoomId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string ColorHex { get; set; } = "#2563EB";
        public string SlideId { get; set; } = "";
        public string ShapeId { get; set; } = "";
        public int SlideIndex { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float W { get; set; }
        public float H { get; set; }
        public float Rotation { get; set; }
        public string SelectedId { get; set; } = "";
        public float CursorX { get; set; }
        public float CursorY { get; set; }
        public bool IsEditing { get; set; }
        public long Seq { get; set; }
    }

    public class CollabRoomOpenData
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public CollabRoomInfo Room { get; set; }
        public byte[] SnapshotBytes { get; set; }
        public long SnapshotVersion { get; set; }
        public long SnapshotMaxOpSeq { get; set; }
        public List<CollabOpRecord> PendingOps { get; set; } = new List<CollabOpRecord>();
        public List<CollabPresenceInfo> Presences { get; set; } = new List<CollabPresenceInfo>();
    }

    public class CollabCreateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public CollabRoomInfo Room { get; set; }
    }

    /// <summary>
    /// Dich vu phong cong tac thoi gian thuc (Realtime Co-editing Rooms) qua PostgreSQL Cloud + LISTEN/NOTIFY.
    /// Ho tro nhieu nguoi cung sua Slides, Calc, Writer; chu phong thoat nguoi khac van tiep tuc sua va luu 5s/lan.
    /// </summary>
    public static class CollabRoomService
    {
        public const int AutoSaveIntervalMs = 5000;

        private static readonly string[] PaletteColors = new[]
        {
            "#2563EB", // Blue
            "#DB2777", // Pink
            "#059669", // Emerald
            "#D97706", // Amber
            "#7C3AED", // Violet
            "#0891B2", // Cyan
            "#DC2626", // Red
            "#4F46E5"  // Indigo
        };

        private static bool _initialized = false;
        private static bool _schemaReady = false;
        private static readonly SemaphoreSlim _schemaGate = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim _roomsRefreshGate = new SemaphoreSlim(1, 1);
        private static readonly HashSet<Guid> _knownRoomIds = new HashSet<Guid>();
        private static bool _knownRoomsLoaded = false;

        private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> _roomListeners
            = new ConcurrentDictionary<Guid, CancellationTokenSource>();
        private static readonly ConcurrentDictionary<string, string> _assetLocalPathCache
            = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static string LocalCacheOverrideDir { get; set; }
        public static List<CollabRoomInfo> MyRooms { get; private set; } = new List<CollabRoomInfo>();
        public static string LastError { get; private set; } = "";

        public static event Action OnRoomsUpdated;
        public static event Action<CollabRoomInfo> OnRoomInvited;
        public static event Action<CollabLiveSignal> OnLiveSignalReceived;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            DatabaseAuthService.OnAuthStateChanged += HandleAuthChanged;
            Task.Run(async () =>
            {
                try
                {
                    await EnsureSchemaAsync().ConfigureAwait(false);
                    if (DatabaseAuthService.IsLoggedIn)
                        await RefreshMyRoomsAsync().ConfigureAwait(false);
                }
                catch { }
            });
        }

        private static void HandleAuthChanged()
        {
            if (!DatabaseAuthService.IsLoggedIn)
            {
                foreach (var kv in _roomListeners)
                {
                    try { kv.Value.Cancel(); } catch { }
                }
                _roomListeners.Clear();
                lock (_knownRoomIds)
                {
                    _knownRoomIds.Clear();
                    _knownRoomsLoaded = false;
                }
                MyRooms = new List<CollabRoomInfo>();
                OnRoomsUpdated?.Invoke();
            }
            else
            {
                Task.Run(async () =>
                {
                    try { await RefreshMyRoomsAsync().ConfigureAwait(false); } catch { }
                });
            }
        }

        public static string GetColorForUser(int userId)
        {
            int idx = Math.Abs(userId) % PaletteColors.Length;
            return PaletteColors[idx];
        }

        public static string GetChannelName(Guid roomId)
        {
            return "zenq_collab_" + roomId.ToString("N");
        }

        public static string GetCollabAssetCacheDir()
        {
            string dir = !string.IsNullOrEmpty(LocalCacheOverrideDir)
                ? Path.Combine(LocalCacheOverrideDir, "CollabAssets")
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ZenQOffice",
                    "CollabAssets");
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static async Task<bool> EnsureSchemaAsync()
        {
            if (_schemaReady) return true;
            await _schemaGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_schemaReady) return true;
                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                string sql = @"
CREATE TABLE IF NOT EXISTS zenq_collab_rooms (
    id UUID PRIMARY KEY,
    room_code VARCHAR(16) NOT NULL UNIQUE,
    title VARCHAR(255) NOT NULL,
    doc_mode INT NOT NULL DEFAULT 3,
    owner_id INT NOT NULL,
    owner_username VARCHAR(100) NOT NULL,
    snapshot_bytes BYTEA,
    snapshot_version BIGINT NOT NULL DEFAULT 1,
    snapshot_op_seq BIGINT NOT NULL DEFAULT 0,
    snapshot_sha256 VARCHAR(64) NOT NULL DEFAULT '',
    updated_by_id INT NOT NULL DEFAULT 0,
    updated_by_username VARCHAR(100) NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE zenq_collab_rooms ADD COLUMN IF NOT EXISTS snapshot_op_seq BIGINT NOT NULL DEFAULT 0;
ALTER TABLE zenq_collab_rooms ADD COLUMN IF NOT EXISTS updated_by_id INT NOT NULL DEFAULT 0;
ALTER TABLE zenq_collab_rooms ADD COLUMN IF NOT EXISTS updated_by_username VARCHAR(100) NOT NULL DEFAULT '';

CREATE TABLE IF NOT EXISTS zenq_collab_members (
    room_id UUID NOT NULL REFERENCES zenq_collab_rooms(id) ON DELETE CASCADE,
    user_id INT NOT NULL,
    username VARCHAR(100) NOT NULL,
    full_name VARCHAR(200) NOT NULL DEFAULT '',
    role VARCHAR(20) NOT NULL DEFAULT 'editor',
    color_hex VARCHAR(16) NOT NULL DEFAULT '#2563EB',
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (room_id, user_id)
);
CREATE INDEX IF NOT EXISTS idx_zenq_collab_members_user ON zenq_collab_members (user_id, room_id);

CREATE TABLE IF NOT EXISTS zenq_collab_ops (
    seq BIGSERIAL PRIMARY KEY,
    room_id UUID NOT NULL REFERENCES zenq_collab_rooms(id) ON DELETE CASCADE,
    user_id INT NOT NULL,
    username VARCHAR(100) NOT NULL,
    client_op_id UUID NOT NULL,
    doc_mode INT NOT NULL,
    op_type VARCHAR(48) NOT NULL,
    target_key VARCHAR(255) NOT NULL DEFAULT '',
    payload_json TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_zenq_collab_ops_room_seq ON zenq_collab_ops (room_id, seq);

CREATE TABLE IF NOT EXISTS zenq_collab_presence (
    room_id UUID NOT NULL REFERENCES zenq_collab_rooms(id) ON DELETE CASCADE,
    user_id INT NOT NULL,
    username VARCHAR(100) NOT NULL,
    full_name VARCHAR(200) NOT NULL DEFAULT '',
    color_hex VARCHAR(16) NOT NULL DEFAULT '#2563EB',
    slide_index INT NOT NULL DEFAULT 0,
    selected_id VARCHAR(255) NOT NULL DEFAULT '',
    cursor_x REAL NOT NULL DEFAULT 0,
    cursor_y REAL NOT NULL DEFAULT 0,
    is_editing BOOLEAN NOT NULL DEFAULT FALSE,
    last_seen TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (room_id, user_id)
);

CREATE TABLE IF NOT EXISTS zenq_collab_assets (
    room_id UUID NOT NULL REFERENCES zenq_collab_rooms(id) ON DELETE CASCADE,
    asset_hash VARCHAR(64) NOT NULL,
    file_ext VARCHAR(16) NOT NULL DEFAULT '.png',
    data BYTEA NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (room_id, asset_hash)
);";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.CommandTimeout = 30;
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                _schemaReady = true;
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
            finally
            {
                _schemaGate.Release();
            }
        }

        private static string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            byte[] rng = new byte[6];
            RandomNumberGenerator.Fill(rng);
            var sb = new StringBuilder("ZQ-", 9);
            for (int i = 0; i < 6; i++)
                sb.Append(chars[rng[i] % chars.Length]);
            return sb.ToString();
        }

        public static string ComputeSha256Hex(byte[] data)
        {
            if (data == null || data.Length == 0) return "";
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(data);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public static async Task<CollabCreateResult> CreateRoomAsync(
            string title,
            OfficeAppMode docMode,
            byte[] initialSnapshotBytes,
            IEnumerable<RegisteredRecipientUser> initialMembers = null)
        {
            if (!DatabaseAuthService.IsLoggedIn || DatabaseAuthService.CurrentUser == null)
            {
                return new CollabCreateResult
                {
                    Success = false,
                    Message = AppL10n.T("Vui lòng đăng nhập tài khoản trước.", "Please sign in first.")
                };
            }
            if (!await EnsureSchemaAsync().ConfigureAwait(false))
            {
                return new CollabCreateResult
                {
                    Success = false,
                    Message = AppL10n.T("Không thể kết nối Cloud: ", "Cloud connection failed: ") + LastError
                };
            }

            try
            {
                var me = DatabaseAuthService.CurrentUser;
                Guid roomId = Guid.NewGuid();
                string roomCode = GenerateRoomCode();
                byte[] bytes = initialSnapshotBytes ?? Array.Empty<byte>();
                string sha = ComputeSha256Hex(bytes);
                string cleanTitle = string.IsNullOrWhiteSpace(title) ? "Tai_lieu_cong_tac" : title.Trim();
                if (cleanTitle.Length > 240) cleanTitle = cleanTitle.Substring(0, 240);

                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);
                using var tx = await conn.BeginTransactionAsync().ConfigureAwait(false);

                using (var insRoom = new NpgsqlCommand(@"
INSERT INTO zenq_collab_rooms (id, room_code, title, doc_mode, owner_id, owner_username, snapshot_bytes, snapshot_version, snapshot_op_seq, snapshot_sha256, updated_by_id, updated_by_username)
VALUES (@id, @code, @title, @mode, @oid, @ouser, @bytes, 1, 0, @sha, @oid, @ouser);", conn, tx))
                {
                    insRoom.Parameters.AddWithValue("id", roomId);
                    insRoom.Parameters.AddWithValue("code", roomCode);
                    insRoom.Parameters.AddWithValue("title", cleanTitle);
                    insRoom.Parameters.AddWithValue("mode", (int)docMode);
                    insRoom.Parameters.AddWithValue("oid", me.Id);
                    insRoom.Parameters.AddWithValue("ouser", me.Username ?? "");
                    insRoom.Parameters.Add("bytes", NpgsqlDbType.Bytea).Value = bytes;
                    insRoom.Parameters.AddWithValue("sha", sha);
                    await insRoom.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                using (var insOwner = new NpgsqlCommand(@"
INSERT INTO zenq_collab_members (room_id, user_id, username, full_name, role, color_hex)
VALUES (@rid, @uid, @uname, @fname, 'owner', @col)
ON CONFLICT (room_id, user_id) DO NOTHING;", conn, tx))
                {
                    insOwner.Parameters.AddWithValue("rid", roomId);
                    insOwner.Parameters.AddWithValue("uid", me.Id);
                    insOwner.Parameters.AddWithValue("uname", me.Username ?? "");
                    insOwner.Parameters.AddWithValue("fname", me.FullName ?? "");
                    insOwner.Parameters.AddWithValue("col", GetColorForUser(me.Id));
                    await insOwner.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                if (initialMembers != null)
                {
                    foreach (var m in initialMembers)
                    {
                        if (m == null || m.Id == me.Id) continue;
                        using var insEd = new NpgsqlCommand(@"
INSERT INTO zenq_collab_members (room_id, user_id, username, full_name, role, color_hex)
VALUES (@rid, @uid, @uname, @fname, 'editor', @col)
ON CONFLICT (room_id, user_id) DO NOTHING;", conn, tx);
                        insEd.Parameters.AddWithValue("rid", roomId);
                        insEd.Parameters.AddWithValue("uid", m.Id);
                        insEd.Parameters.AddWithValue("uname", m.Username ?? "");
                        insEd.Parameters.AddWithValue("fname", m.FullName ?? "");
                        insEd.Parameters.AddWithValue("col", GetColorForUser(m.Id));
                        await insEd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                }

                await tx.CommitAsync().ConfigureAwait(false);
                lock (_knownRoomIds) _knownRoomIds.Add(roomId);
                await RefreshMyRoomsAsync().ConfigureAwait(false);

                var created = MyRooms.FirstOrDefault(r => r.Id == roomId) ?? new CollabRoomInfo
                {
                    Id = roomId,
                    RoomCode = roomCode,
                    Title = cleanTitle,
                    DocMode = (int)docMode,
                    OwnerId = me.Id,
                    OwnerUsername = me.Username ?? "",
                    SnapshotVersion = 1,
                    SnapshotSha256 = sha,
                    SnapshotSize = bytes.Length,
                    MyRole = "owner",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                return new CollabCreateResult
                {
                    Success = true,
                    Message = AppL10n.T("Đã tạo phòng cộng tác thành công!", "Collaboration room created!"),
                    Room = created
                };
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return new CollabCreateResult
                {
                    Success = false,
                    Message = AppL10n.T("Lỗi tạo phòng: ", "Failed to create room: ") + ex.Message
                };
            }
        }

        public static async Task<(bool Ok, string Message)> InviteMemberAsync(Guid roomId, string recipientUsernameOrEmail)
        {
            if (!DatabaseAuthService.IsLoggedIn || DatabaseAuthService.CurrentUser == null)
                return (false, AppL10n.T("Chưa đăng nhập.", "Not signed in."));

            var target = await CloudShareService.FindRegisteredUserAsync(recipientUsernameOrEmail).ConfigureAwait(false);
            if (target == null)
                return (false, AppL10n.T("Không tìm thấy tài khoản nhận.", "Account not found."));
            if (target.Id == DatabaseAuthService.CurrentUser.Id)
                return (false, AppL10n.T("Tài khoản này đang là bạn.", "Cannot invite yourself."));

            try
            {
                await EnsureSchemaAsync().ConfigureAwait(false);
                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                using var cmd = new NpgsqlCommand(@"
INSERT INTO zenq_collab_members (room_id, user_id, username, full_name, role, color_hex)
VALUES (@rid, @uid, @uname, @fname, 'editor', @col)
ON CONFLICT (room_id, user_id) DO NOTHING;", conn);
                cmd.Parameters.AddWithValue("rid", roomId);
                cmd.Parameters.AddWithValue("uid", target.Id);
                cmd.Parameters.AddWithValue("uname", target.Username ?? "");
                cmd.Parameters.AddWithValue("fname", target.FullName ?? "");
                cmd.Parameters.AddWithValue("col", GetColorForUser(target.Id));
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                await RefreshMyRoomsAsync().ConfigureAwait(false);
                return (true, string.Format(AppL10n.T("Đã mời @{0} vào cùng chỉnh sửa.", "Invited @{0} to collaborate."), target.Username));
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static async Task<CollabCreateResult> JoinByRoomCodeAsync(string roomCode)
        {
            if (!DatabaseAuthService.IsLoggedIn || DatabaseAuthService.CurrentUser == null)
                return new CollabCreateResult { Success = false, Message = AppL10n.T("Vui lòng đăng nhập.", "Please sign in.") };

            string code = (roomCode ?? "").Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(code) && !code.StartsWith("ZQ-") && code.Length == 6)
                code = "ZQ-" + code;
            if (string.IsNullOrEmpty(code))
                return new CollabCreateResult { Success = false, Message = AppL10n.T("Mã phòng không hợp lệ.", "Invalid room code.") };

            try
            {
                await EnsureSchemaAsync().ConfigureAwait(false);
                var me = DatabaseAuthService.CurrentUser;
                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                Guid roomId = Guid.Empty;
                using (var findCmd = new NpgsqlCommand("SELECT id FROM zenq_collab_rooms WHERE UPPER(room_code) = @code LIMIT 1;", conn))
                {
                    findCmd.Parameters.AddWithValue("code", code);
                    object res = await findCmd.ExecuteScalarAsync().ConfigureAwait(false);
                    if (res == null || res == DBNull.Value)
                        return new CollabCreateResult { Success = false, Message = AppL10n.T("Không tìm thấy phòng với mã này.", "Room not found.") };
                    roomId = (Guid)res;
                }

                using (var addCmd = new NpgsqlCommand(@"
INSERT INTO zenq_collab_members (room_id, user_id, username, full_name, role, color_hex)
VALUES (@rid, @uid, @uname, @fname, 'editor', @col)
ON CONFLICT (room_id, user_id) DO NOTHING;", conn))
                {
                    addCmd.Parameters.AddWithValue("rid", roomId);
                    addCmd.Parameters.AddWithValue("uid", me.Id);
                    addCmd.Parameters.AddWithValue("uname", me.Username ?? "");
                    addCmd.Parameters.AddWithValue("fname", me.FullName ?? "");
                    addCmd.Parameters.AddWithValue("col", GetColorForUser(me.Id));
                    await addCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                await RefreshMyRoomsAsync().ConfigureAwait(false);
                var joined = MyRooms.FirstOrDefault(r => r.Id == roomId);
                return new CollabCreateResult
                {
                    Success = true,
                    Message = AppL10n.T("Đã tham gia phòng cộng tác!", "Joined collaboration room!"),
                    Room = joined
                };
            }
            catch (Exception ex)
            {
                return new CollabCreateResult { Success = false, Message = ex.Message };
            }
        }

        public static async Task<List<CollabRoomInfo>> RefreshMyRoomsAsync()
        {
            if (!DatabaseAuthService.IsLoggedIn || DatabaseAuthService.CurrentUser == null)
                return new List<CollabRoomInfo>();

            if (!await _roomsRefreshGate.WaitAsync(0).ConfigureAwait(false))
                return MyRooms;

            try
            {
                if (!await EnsureSchemaAsync().ConfigureAwait(false))
                    return MyRooms;

                int uid = DatabaseAuthService.CurrentUser.Id;
                var list = new List<CollabRoomInfo>();
                var roomMap = new Dictionary<Guid, CollabRoomInfo>();

                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                using (var cmd = new NpgsqlCommand(@"
SELECT r.id, r.room_code, r.title, r.doc_mode, r.owner_id, r.owner_username,
       r.snapshot_version, r.snapshot_sha256, COALESCE(OCTET_LENGTH(r.snapshot_bytes), 0),
       r.updated_by_username, r.created_at, r.updated_at, m.role
FROM zenq_collab_rooms r
JOIN zenq_collab_members m ON m.room_id = r.id
WHERE m.user_id = @uid
ORDER BY r.updated_at DESC
LIMIT 60;", conn))
                {
                    cmd.Parameters.AddWithValue("uid", uid);
                    using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                    while (await rd.ReadAsync().ConfigureAwait(false))
                    {
                        var room = new CollabRoomInfo
                        {
                            Id = rd.GetGuid(0),
                            RoomCode = rd.GetString(1),
                            Title = rd.GetString(2),
                            DocMode = rd.GetInt32(3),
                            OwnerId = rd.GetInt32(4),
                            OwnerUsername = rd.GetString(5),
                            SnapshotVersion = rd.GetInt64(6),
                            SnapshotSha256 = rd.IsDBNull(7) ? "" : rd.GetString(7),
                            SnapshotSize = rd.GetInt64(8),
                            UpdatedByUsername = rd.IsDBNull(9) ? "" : rd.GetString(9),
                            CreatedAt = rd.GetDateTime(10),
                            UpdatedAt = rd.GetDateTime(11),
                            MyRole = rd.GetString(12)
                        };
                        list.Add(room);
                        roomMap[room.Id] = room;
                    }
                }

                if (list.Count > 0)
                {
                    var ids = roomMap.Keys.ToArray();
                    using (var mCmd = new NpgsqlCommand(@"
SELECT room_id, user_id, username, full_name, role, color_hex, joined_at
FROM zenq_collab_members
WHERE room_id = ANY(@ids)
ORDER BY joined_at ASC;", conn))
                    {
                        mCmd.Parameters.AddWithValue("ids", ids);
                        using var rd = await mCmd.ExecuteReaderAsync().ConfigureAwait(false);
                        while (await rd.ReadAsync().ConfigureAwait(false))
                        {
                            Guid rid = rd.GetGuid(0);
                            if (roomMap.TryGetValue(rid, out var r))
                            {
                                r.Members.Add(new CollabMemberInfo
                                {
                                    UserId = rd.GetInt32(1),
                                    Username = rd.GetString(2),
                                    FullName = rd.IsDBNull(3) ? "" : rd.GetString(3),
                                    Role = rd.GetString(4),
                                    ColorHex = rd.IsDBNull(5) ? "#2563EB" : rd.GetString(5),
                                    JoinedAt = rd.GetDateTime(6)
                                });
                            }
                        }
                    }

                    using (var pCmd = new NpgsqlCommand(@"
SELECT room_id, COUNT(*)
FROM zenq_collab_presence
WHERE room_id = ANY(@ids) AND last_seen > NOW() - INTERVAL '15 seconds'
GROUP BY room_id;", conn))
                    {
                        pCmd.Parameters.AddWithValue("ids", ids);
                        using var rd = await pCmd.ExecuteReaderAsync().ConfigureAwait(false);
                        while (await rd.ReadAsync().ConfigureAwait(false))
                        {
                            Guid rid = rd.GetGuid(0);
                            if (roomMap.TryGetValue(rid, out var r))
                                r.OnlineCount = rd.GetInt32(1);
                        }
                    }
                }

                List<CollabRoomInfo> newlyInvited = new List<CollabRoomInfo>();
                lock (_knownRoomIds)
                {
                    if (!_knownRoomsLoaded)
                    {
                        foreach (var r in list) _knownRoomIds.Add(r.Id);
                        _knownRoomsLoaded = true;
                    }
                    else
                    {
                        foreach (var r in list)
                        {
                            if (_knownRoomIds.Add(r.Id) && r.OwnerId != uid)
                                newlyInvited.Add(r);
                        }
                    }
                }

                MyRooms = list;
                OnRoomsUpdated?.Invoke();
                foreach (var inv in newlyInvited)
                    OnRoomInvited?.Invoke(inv);

                return list;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return MyRooms;
            }
            finally
            {
                _roomsRefreshGate.Release();
            }
        }

        public static async Task<bool> LeaveOrDeleteRoomAsync(Guid roomId)
        {
            if (!DatabaseAuthService.IsLoggedIn || DatabaseAuthService.CurrentUser == null) return false;
            try
            {
                int uid = DatabaseAuthService.CurrentUser.Id;
                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                int ownerId = 0;
                using (var q = new NpgsqlCommand("SELECT owner_id FROM zenq_collab_rooms WHERE id = @rid;", conn))
                {
                    q.Parameters.AddWithValue("rid", roomId);
                    object o = await q.ExecuteScalarAsync().ConfigureAwait(false);
                    if (o == null || o == DBNull.Value) return true;
                    ownerId = Convert.ToInt32(o);
                }

                if (ownerId == uid)
                {
                    using var del = new NpgsqlCommand("DELETE FROM zenq_collab_rooms WHERE id = @rid;", conn);
                    del.Parameters.AddWithValue("rid", roomId);
                    await del.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                else
                {
                    using var delM = new NpgsqlCommand("DELETE FROM zenq_collab_members WHERE room_id = @rid AND user_id = @uid;", conn);
                    delM.Parameters.AddWithValue("rid", roomId);
                    delM.Parameters.AddWithValue("uid", uid);
                    await delM.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                StopRoomSubscription(roomId);
                await RefreshMyRoomsAsync().ConfigureAwait(false);
                return true;
            }
            catch { return false; }
        }

        public static async Task<CollabRoomOpenData> OpenRoomSnapshotAsync(Guid roomId)
        {
            if (!DatabaseAuthService.IsLoggedIn || DatabaseAuthService.CurrentUser == null)
                return new CollabRoomOpenData { Success = false, Message = AppL10n.T("Vui lòng đăng nhập.", "Please sign in.") };

            try
            {
                await EnsureSchemaAsync().ConfigureAwait(false);
                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                CollabRoomInfo room = null;
                byte[] snapBytes = null;
                long snapOpSeq = 0;

                using (var cmd = new NpgsqlCommand(@"
SELECT r.id, r.room_code, r.title, r.doc_mode, r.owner_id, r.owner_username,
       r.snapshot_bytes, r.snapshot_version, r.snapshot_op_seq, r.snapshot_sha256,
       r.updated_by_username, r.created_at, r.updated_at,
       COALESCE((SELECT m.role FROM zenq_collab_members m WHERE m.room_id = r.id AND m.user_id = @uid LIMIT 1), 'editor')
FROM zenq_collab_rooms r
WHERE r.id = @rid;", conn))
                {
                    cmd.Parameters.AddWithValue("rid", roomId);
                    cmd.Parameters.AddWithValue("uid", DatabaseAuthService.CurrentUser.Id);
                    using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                    if (!await rd.ReadAsync().ConfigureAwait(false))
                    {
                        return new CollabRoomOpenData
                        {
                            Success = false,
                            Message = AppL10n.T("Phòng cộng tác không tồn tại hoặc đã bị xóa.", "Room does not exist.")
                        };
                    }
                    snapBytes = rd.IsDBNull(6) ? Array.Empty<byte>() : (byte[])rd[6];
                    snapOpSeq = rd.IsDBNull(8) ? 0 : rd.GetInt64(8);
                    room = new CollabRoomInfo
                    {
                        Id = rd.GetGuid(0),
                        RoomCode = rd.GetString(1),
                        Title = rd.GetString(2),
                        DocMode = rd.GetInt32(3),
                        OwnerId = rd.GetInt32(4),
                        OwnerUsername = rd.GetString(5),
                        SnapshotVersion = rd.GetInt64(7),
                        SnapshotSha256 = rd.IsDBNull(9) ? "" : rd.GetString(9),
                        SnapshotSize = snapBytes.Length,
                        UpdatedByUsername = rd.IsDBNull(10) ? "" : rd.GetString(10),
                        CreatedAt = rd.GetDateTime(11),
                        UpdatedAt = rd.GetDateTime(12),
                        MyRole = rd.GetString(13)
                    };
                }

                // Load members
                using (var mCmd = new NpgsqlCommand(@"
SELECT user_id, username, full_name, role, color_hex, joined_at
FROM zenq_collab_members WHERE room_id = @rid ORDER BY joined_at ASC;", conn))
                {
                    mCmd.Parameters.AddWithValue("rid", roomId);
                    using var rd = await mCmd.ExecuteReaderAsync().ConfigureAwait(false);
                    while (await rd.ReadAsync().ConfigureAwait(false))
                    {
                        room.Members.Add(new CollabMemberInfo
                        {
                            UserId = rd.GetInt32(0),
                            Username = rd.GetString(1),
                            FullName = rd.IsDBNull(2) ? "" : rd.GetString(2),
                            Role = rd.GetString(3),
                            ColorHex = rd.IsDBNull(4) ? "#2563EB" : rd.GetString(4),
                            JoinedAt = rd.GetDateTime(5)
                        });
                    }
                }

                // Load pending ops after snapshot_op_seq so even edits made in the last <5s before reload are replayed cleanly
                var pendingOps = await FetchOpsAfterSeqInternalAsync(conn, roomId, snapOpSeq).ConfigureAwait(false);
                var presences = await FetchPresencesInternalAsync(conn, roomId).ConfigureAwait(false);

                return new CollabRoomOpenData
                {
                    Success = true,
                    Room = room,
                    SnapshotBytes = snapBytes,
                    SnapshotVersion = room.SnapshotVersion,
                    SnapshotMaxOpSeq = snapOpSeq,
                    PendingOps = pendingOps,
                    Presences = presences
                };
            }
            catch (Exception ex)
            {
                return new CollabRoomOpenData { Success = false, Message = ex.Message };
            }
        }

        private static async Task<List<CollabOpRecord>> FetchOpsAfterSeqInternalAsync(NpgsqlConnection conn, Guid roomId, long afterSeq)
        {
            var ops = new List<CollabOpRecord>();
            using var cmd = new NpgsqlCommand(@"
SELECT seq, room_id, user_id, username, client_op_id, doc_mode, op_type, target_key, payload_json, created_at
FROM zenq_collab_ops
WHERE room_id = @rid AND seq > @seq
ORDER BY seq ASC
LIMIT 400;", conn);
            cmd.Parameters.AddWithValue("rid", roomId);
            cmd.Parameters.AddWithValue("seq", afterSeq);
            using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await rd.ReadAsync().ConfigureAwait(false))
            {
                ops.Add(new CollabOpRecord
                {
                    Seq = rd.GetInt64(0),
                    RoomId = rd.GetGuid(1),
                    UserId = rd.GetInt32(2),
                    Username = rd.GetString(3),
                    ClientOpId = rd.GetGuid(4),
                    DocMode = rd.GetInt32(5),
                    OpType = rd.GetString(6),
                    TargetKey = rd.IsDBNull(7) ? "" : rd.GetString(7),
                    PayloadJson = rd.GetString(8),
                    CreatedAt = rd.GetDateTime(9)
                });
            }
            return ops;
        }

        private static async Task<List<CollabPresenceInfo>> FetchPresencesInternalAsync(NpgsqlConnection conn, Guid roomId)
        {
            var list = new List<CollabPresenceInfo>();
            using var cmd = new NpgsqlCommand(@"
SELECT room_id, user_id, username, full_name, color_hex, slide_index, selected_id, cursor_x, cursor_y, is_editing, last_seen
FROM zenq_collab_presence
WHERE room_id = @rid AND last_seen > NOW() - INTERVAL '25 seconds';", conn);
            cmd.Parameters.AddWithValue("rid", roomId);
            using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await rd.ReadAsync().ConfigureAwait(false))
            {
                list.Add(new CollabPresenceInfo
                {
                    RoomId = rd.GetGuid(0),
                    UserId = rd.GetInt32(1),
                    Username = rd.GetString(2),
                    FullName = rd.IsDBNull(3) ? "" : rd.GetString(3),
                    ColorHex = rd.IsDBNull(4) ? "#2563EB" : rd.GetString(4),
                    SlideIndex = rd.GetInt32(5),
                    SelectedId = rd.IsDBNull(6) ? "" : rd.GetString(6),
                    CursorX = rd.GetFloat(7),
                    CursorY = rd.GetFloat(8),
                    IsEditing = rd.GetBoolean(9),
                    LastSeen = rd.GetDateTime(10)
                });
            }
            return list;
        }

        public static async Task<(List<CollabOpRecord> Ops, List<CollabPresenceInfo> Presences, long SnapshotVersion, bool RoomExists)> PollRoomStateAsync(Guid roomId, long afterSeq)
        {
            if (!DatabaseAuthService.IsLoggedIn) return (new List<CollabOpRecord>(), new List<CollabPresenceInfo>(), 0, true);
            try
            {
                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                long snapVer = 0;
                bool exists = false;
                using (var vCmd = new NpgsqlCommand("SELECT snapshot_version FROM zenq_collab_rooms WHERE id = @rid;", conn))
                {
                    vCmd.Parameters.AddWithValue("rid", roomId);
                    object v = await vCmd.ExecuteScalarAsync().ConfigureAwait(false);
                    if (v != null && v != DBNull.Value)
                    {
                        exists = true;
                        snapVer = Convert.ToInt64(v);
                    }
                }
                if (!exists)
                    return (new List<CollabOpRecord>(), new List<CollabPresenceInfo>(), 0, false);

                var ops = await FetchOpsAfterSeqInternalAsync(conn, roomId, afterSeq).ConfigureAwait(false);
                var presences = await FetchPresencesInternalAsync(conn, roomId).ConfigureAwait(false);
                return (ops, presences, snapVer, true);
            }
            catch
            {
                return (new List<CollabOpRecord>(), new List<CollabPresenceInfo>(), 0, true);
            }
        }

        public static async Task<long> PushOpAsync(Guid roomId, Guid clientOpId, int docMode, string opType, string targetKey, string payloadJson)
        {
            if (!DatabaseAuthService.IsLoggedIn || DatabaseAuthService.CurrentUser == null) return 0;
            try
            {
                var me = DatabaseAuthService.CurrentUser;
                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                long seq = 0;
                using (var cmd = new NpgsqlCommand(@"
INSERT INTO zenq_collab_ops (room_id, user_id, username, client_op_id, doc_mode, op_type, target_key, payload_json)
VALUES (@rid, @uid, @uname, @cid, @mode, @op, @key, @payload)
RETURNING seq;", conn))
                {
                    cmd.Parameters.AddWithValue("rid", roomId);
                    cmd.Parameters.AddWithValue("uid", me.Id);
                    cmd.Parameters.AddWithValue("uname", me.Username ?? "");
                    cmd.Parameters.AddWithValue("cid", clientOpId);
                    cmd.Parameters.AddWithValue("mode", docMode);
                    cmd.Parameters.AddWithValue("op", opType ?? "");
                    cmd.Parameters.AddWithValue("key", targetKey ?? "");
                    cmd.Parameters.AddWithValue("payload", payloadJson ?? "{}");
                    object res = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                    if (res != null && res != DBNull.Value) seq = Convert.ToInt64(res);
                }

                // Send lightweight instant notification via PostgreSQL NOTIFY
                var ping = new CollabLiveSignal
                {
                    Type = "op_ping",
                    RoomId = roomId,
                    UserId = me.Id,
                    Username = me.Username ?? "",
                    Seq = seq
                };
                string json = JsonSerializer.Serialize(ping);
                if (json.Length < 7500)
                {
                    using var nCmd = new NpgsqlCommand("SELECT pg_notify(@ch, @msg);", conn);
                    nCmd.Parameters.AddWithValue("ch", GetChannelName(roomId));
                    nCmd.Parameters.AddWithValue("msg", json);
                    await nCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                return seq;
            }
            catch { return 0; }
        }

        public static async Task PushLiveSignalAsync(Guid roomId, CollabLiveSignal signal)
        {
            if (signal == null || !DatabaseAuthService.IsLoggedIn || DatabaseAuthService.CurrentUser == null) return;
            try
            {
                var me = DatabaseAuthService.CurrentUser;
                signal.RoomId = roomId;
                signal.UserId = me.Id;
                signal.Username = me.Username ?? "";
                signal.ColorHex = GetColorForUser(me.Id);

                string json = JsonSerializer.Serialize(signal);
                if (json.Length >= 7500) return;

                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);
                using var cmd = new NpgsqlCommand("SELECT pg_notify(@ch, @msg);", conn);
                cmd.Parameters.AddWithValue("ch", GetChannelName(roomId));
                cmd.Parameters.AddWithValue("msg", json);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch { }
        }

        public static async Task UpsertPresenceAsync(Guid roomId, int slideIndex, string selectedId, float cursorX, float cursorY, bool isEditing)
        {
            if (!DatabaseAuthService.IsLoggedIn || DatabaseAuthService.CurrentUser == null) return;
            try
            {
                var me = DatabaseAuthService.CurrentUser;
                string colHex = GetColorForUser(me.Id);
                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                using (var cmd = new NpgsqlCommand(@"
INSERT INTO zenq_collab_presence (room_id, user_id, username, full_name, color_hex, slide_index, selected_id, cursor_x, cursor_y, is_editing, last_seen)
VALUES (@rid, @uid, @uname, @fname, @col, @si, @sid, @cx, @cy, @ed, NOW())
ON CONFLICT (room_id, user_id) DO UPDATE SET
    username = EXCLUDED.username,
    full_name = EXCLUDED.full_name,
    color_hex = EXCLUDED.color_hex,
    slide_index = EXCLUDED.slide_index,
    selected_id = EXCLUDED.selected_id,
    cursor_x = EXCLUDED.cursor_x,
    cursor_y = EXCLUDED.cursor_y,
    is_editing = EXCLUDED.is_editing,
    last_seen = NOW();", conn))
                {
                    cmd.Parameters.AddWithValue("rid", roomId);
                    cmd.Parameters.AddWithValue("uid", me.Id);
                    cmd.Parameters.AddWithValue("uname", me.Username ?? "");
                    cmd.Parameters.AddWithValue("fname", me.FullName ?? "");
                    cmd.Parameters.AddWithValue("col", colHex);
                    cmd.Parameters.AddWithValue("si", slideIndex);
                    cmd.Parameters.AddWithValue("sid", selectedId ?? "");
                    cmd.Parameters.AddWithValue("cx", cursorX);
                    cmd.Parameters.AddWithValue("cy", cursorY);
                    cmd.Parameters.AddWithValue("ed", isEditing);
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                var sig = new CollabLiveSignal
                {
                    Type = "presence",
                    RoomId = roomId,
                    UserId = me.Id,
                    Username = me.Username ?? "",
                    ColorHex = colHex,
                    SlideIndex = slideIndex,
                    SelectedId = selectedId ?? "",
                    CursorX = cursorX,
                    CursorY = cursorY,
                    IsEditing = isEditing
                };
                string json = JsonSerializer.Serialize(sig);
                if (json.Length < 7500)
                {
                    using var nCmd = new NpgsqlCommand("SELECT pg_notify(@ch, @msg);", conn);
                    nCmd.Parameters.AddWithValue("ch", GetChannelName(roomId));
                    nCmd.Parameters.AddWithValue("msg", json);
                    await nCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
            catch { }
        }

        public static async Task RemovePresenceAsync(Guid roomId)
        {
            if (!DatabaseAuthService.IsLoggedIn || DatabaseAuthService.CurrentUser == null) return;
            try
            {
                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);
                using var cmd = new NpgsqlCommand("DELETE FROM zenq_collab_presence WHERE room_id = @rid AND user_id = @uid;", conn);
                cmd.Parameters.AddWithValue("rid", roomId);
                cmd.Parameters.AddWithValue("uid", DatabaseAuthService.CurrentUser.Id);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch { }
        }

        public static async Task<(bool Ok, long NewVersion)> SaveSnapshotAsync(Guid roomId, byte[] snapshotBytes, long maxAppliedOpSeq)
        {
            if (!DatabaseAuthService.IsLoggedIn || DatabaseAuthService.CurrentUser == null || snapshotBytes == null)
                return (false, 0);

            try
            {
                var me = DatabaseAuthService.CurrentUser;
                string sha = ComputeSha256Hex(snapshotBytes);

                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                long newVer = 0;
                using (var cmd = new NpgsqlCommand(@"
UPDATE zenq_collab_rooms
SET snapshot_bytes = @bytes,
    snapshot_version = snapshot_version + 1,
    snapshot_op_seq = GREATEST(snapshot_op_seq, @opseq),
    snapshot_sha256 = @sha,
    updated_by_id = @uid,
    updated_by_username = @uname,
    updated_at = NOW()
WHERE id = @rid
RETURNING snapshot_version;", conn))
                {
                    cmd.Parameters.Add("bytes", NpgsqlDbType.Bytea).Value = snapshotBytes;
                    cmd.Parameters.AddWithValue("opseq", maxAppliedOpSeq);
                    cmd.Parameters.AddWithValue("sha", sha);
                    cmd.Parameters.AddWithValue("uid", me.Id);
                    cmd.Parameters.AddWithValue("uname", me.Username ?? "");
                    cmd.Parameters.AddWithValue("rid", roomId);
                    object v = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                    if (v != null && v != DBNull.Value) newVer = Convert.ToInt64(v);
                }

                // Prune very old ops already folded into snapshot (keep recent 400 ops for safety)
                using (var prune = new NpgsqlCommand(@"
DELETE FROM zenq_collab_ops
WHERE room_id = @rid
  AND seq < @opseq
  AND seq NOT IN (
      SELECT seq FROM zenq_collab_ops WHERE room_id = @rid ORDER BY seq DESC LIMIT 400
  );", conn))
                {
                    prune.Parameters.AddWithValue("rid", roomId);
                    prune.Parameters.AddWithValue("opseq", maxAppliedOpSeq);
                    await prune.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                return (newVer > 0, newVer);
            }
            catch
            {
                return (false, 0);
            }
        }

        public static async Task<string> EnsureAssetUploadedAsync(Guid roomId, string localFilePath)
        {
            if (roomId == Guid.Empty || string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
                return "";

            try
            {
                byte[] data = await File.ReadAllBytesAsync(localFilePath).ConfigureAwait(false);
                if (data.Length == 0 || data.Length > 15 * 1024 * 1024) return "";
                string hash = ComputeSha256Hex(data);
                string ext = Path.GetExtension(localFilePath);
                if (string.IsNullOrEmpty(ext)) ext = ".png";

                string cachePath = Path.Combine(GetCollabAssetCacheDir(), hash + ext);
                _assetLocalPathCache[hash] = cachePath;
                if (!File.Exists(cachePath))
                {
                    try { await File.WriteAllBytesAsync(cachePath, data).ConfigureAwait(false); } catch { }
                }

                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);
                using var cmd = new NpgsqlCommand(@"
INSERT INTO zenq_collab_assets (room_id, asset_hash, file_ext, data)
VALUES (@rid, @hash, @ext, @data)
ON CONFLICT (room_id, asset_hash) DO NOTHING;", conn);
                cmd.Parameters.AddWithValue("rid", roomId);
                cmd.Parameters.AddWithValue("hash", hash);
                cmd.Parameters.AddWithValue("ext", ext);
                cmd.Parameters.Add("data", NpgsqlDbType.Bytea).Value = data;
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                return hash + ext;
            }
            catch { return ""; }
        }

        public static async Task<string> EnsureAssetDownloadedAsync(Guid roomId, string assetKey)
        {
            if (roomId == Guid.Empty || string.IsNullOrEmpty(assetKey)) return "";
            string hash = Path.GetFileNameWithoutExtension(assetKey);
            string ext = Path.GetExtension(assetKey);
            if (string.IsNullOrEmpty(hash)) return "";
            if (string.IsNullOrEmpty(ext)) ext = ".png";

            if (_assetLocalPathCache.TryGetValue(hash, out string cached) && File.Exists(cached))
                return cached;

            string localPath = Path.Combine(GetCollabAssetCacheDir(), hash + ext);
            if (File.Exists(localPath))
            {
                _assetLocalPathCache[hash] = localPath;
                return localPath;
            }

            try
            {
                using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);
                using var cmd = new NpgsqlCommand("SELECT file_ext, data FROM zenq_collab_assets WHERE room_id = @rid AND asset_hash = @hash LIMIT 1;", conn);
                cmd.Parameters.AddWithValue("rid", roomId);
                cmd.Parameters.AddWithValue("hash", hash);
                using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                if (await rd.ReadAsync().ConfigureAwait(false))
                {
                    string dbExt = rd.IsDBNull(0) ? ext : rd.GetString(0);
                    byte[] bytes = (byte[])rd[1];
                    string outPath = Path.Combine(GetCollabAssetCacheDir(), hash + dbExt);
                    await File.WriteAllBytesAsync(outPath, bytes).ConfigureAwait(false);
                    _assetLocalPathCache[hash] = outPath;
                    return outPath;
                }
            }
            catch { }
            return "";
        }

        public static void StartRoomSubscription(Guid roomId)
        {
            if (roomId == Guid.Empty || _roomListeners.ContainsKey(roomId)) return;
            var cts = new CancellationTokenSource();
            if (!_roomListeners.TryAdd(roomId, cts))
            {
                cts.Dispose();
                return;
            }

            string channel = GetChannelName(roomId);
            CancellationToken ct = cts.Token;

            Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        using var conn = new NpgsqlConnection(DatabaseAuthService.CONNECTION_STRING);
                        await conn.OpenAsync(ct).ConfigureAwait(false);
                        conn.Notification += (sender, args) =>
                        {
                            try
                            {
                                if (string.IsNullOrWhiteSpace(args.Payload)) return;
                                var sig = JsonSerializer.Deserialize<CollabLiveSignal>(args.Payload);
                                if (sig != null)
                                    OnLiveSignalReceived?.Invoke(sig);
                            }
                            catch { }
                        };

                        using (var listenCmd = new NpgsqlCommand($"LISTEN \"{channel}\";", conn))
                        {
                            await listenCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                        }

                        while (!ct.IsCancellationRequested && conn.State == System.Data.ConnectionState.Open)
                        {
                            await conn.WaitAsync(ct).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        try { await Task.Delay(1500, ct).ConfigureAwait(false); } catch { break; }
                    }
                }
            }, ct);
        }

        public static void StopRoomSubscription(Guid roomId)
        {
            if (_roomListeners.TryRemove(roomId, out var cts))
            {
                try { cts.Cancel(); cts.Dispose(); } catch { }
            }
        }
    }
}

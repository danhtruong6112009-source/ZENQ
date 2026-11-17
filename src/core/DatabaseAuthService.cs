using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;

namespace ZenQOffice.Core
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; } = "user"; // "admin", "creator", "user"
        public string AvatarUrl { get; set; }
        public string Bio { get; set; }
        public bool Ban { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsAdmin => string.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase);

        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsCreator => string.Equals(Role, "creator", StringComparison.OrdinalIgnoreCase) || IsAdmin;

        [System.Text.Json.Serialization.JsonIgnore]
        public string RoleDisplayName => Role switch
        {
            "admin" => AppL10n.T("Quản trị viên", "Administrator"),
            "creator" => AppL10n.T("Nhà sáng tạo", "Creator"),
            _ => AppL10n.T("Thành viên", "Member")
        };

        [System.Text.Json.Serialization.JsonIgnore]
        public string Initials
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(FullName))
                {
                    string[] parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
                    return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpperInvariant();
                }
                if (!string.IsNullOrWhiteSpace(Username))
                {
                    return Username.Substring(0, Math.Min(2, Username.Length)).ToUpperInvariant();
                }
                return "ZQ";
            }
        }
    }

    public static class DatabaseAuthService
    {
        public const string CONNECTION_STRING = "Host=zenq-danhtruong6112009-4dcc.f.aivencloud.com;Port=23101;Database=defaultdb;Username=avnadmin;Password=AVNS_f5WllpAXVY113UtrnxR;SSL Mode=Require;Trust Server Certificate=true";

        public static UserProfile CurrentUser { get; private set; }
        public static bool IsLoggedIn => CurrentUser != null;

        public static event Action OnAuthStateChanged;

        private static bool _initialized = false;
        private static readonly object _lock = new object();

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // Nạp phiên đăng nhập đã lưu từ tệp cục bộ
            LoadSavedSession();

            // Kiểm tra kết nối cơ sở dữ liệu ngầm và cập nhật thông tin mới nhất nếu đã đăng nhập
            Task.Run(async () =>
            {
                try
                {
                    await EnsureTablesCreatedAsync().ConfigureAwait(false);
                    if (CurrentUser != null)
                    {
                        await RefreshCurrentUserAsync().ConfigureAwait(false);
                    }
                }
                catch { }
            });
        }

        private static string GetSessionFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "GenOffice");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "user_session.json");
        }

        private static string GetBackupSessionFilePath()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(baseDir, "user_session.json");
            }
            catch
            {
                return null;
            }
        }

        private static void LoadSavedSession()
        {
            try
            {
                string primaryPath = GetSessionFilePath();
                string backupPath = GetBackupSessionFilePath();
                string json = null;

                if (File.Exists(primaryPath))
                {
                    try { json = File.ReadAllText(primaryPath).Trim('\uFEFF', ' ', '\r', '\n', '\t'); } catch { }
                }

                if (string.IsNullOrWhiteSpace(json) && !string.IsNullOrEmpty(backupPath) && File.Exists(backupPath))
                {
                    try { json = File.ReadAllText(backupPath).Trim('\uFEFF', ' ', '\r', '\n', '\t'); } catch { }
                }

                if (!string.IsNullOrWhiteSpace(json))
                {
                    var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var user = JsonSerializer.Deserialize<UserProfile>(json, opt);
                    if (user != null && user.Id > 0)
                    {
                        CurrentUser = user;
                        OnAuthStateChanged?.Invoke();

                        // Đồng bộ sang vị trí còn thiếu nếu cần
                        if (!string.IsNullOrEmpty(backupPath) && !File.Exists(backupPath))
                        {
                            try { File.WriteAllText(backupPath, json, new UTF8Encoding(false)); } catch { }
                        }
                        if (!File.Exists(primaryPath))
                        {
                            try { File.WriteAllText(primaryPath, json, new UTF8Encoding(false)); } catch { }
                        }
                    }
                }
            }
            catch { }
        }

        private static void SaveSession(UserProfile user)
        {
            try
            {
                string primaryPath = GetSessionFilePath();
                string backupPath = GetBackupSessionFilePath();

                if (user == null)
                {
                    try { if (File.Exists(primaryPath)) File.Delete(primaryPath); } catch { }
                    try { if (!string.IsNullOrEmpty(backupPath) && File.Exists(backupPath)) File.Delete(backupPath); } catch { }
                }
                else
                {
                    var opt = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };
                    string json = JsonSerializer.Serialize(user, opt);

                    // Ghi tệp chính an toàn bằng tệp tạm .tmp tránh gián đoạn
                    try
                    {
                        string tmpPrimary = primaryPath + ".tmp";
                        File.WriteAllText(tmpPrimary, json, new UTF8Encoding(false));
                        if (File.Exists(primaryPath)) File.Delete(primaryPath);
                        File.Move(tmpPrimary, primaryPath);
                    }
                    catch
                    {
                        File.WriteAllText(primaryPath, json, new UTF8Encoding(false));
                    }

                    // Ghi tệp dự phòng tại thư mục ứng dụng
                    if (!string.IsNullOrEmpty(backupPath))
                    {
                        try
                        {
                            string tmpBackup = backupPath + ".tmp";
                            File.WriteAllText(tmpBackup, json, new UTF8Encoding(false));
                            if (File.Exists(backupPath)) File.Delete(backupPath);
                            File.Move(tmpBackup, backupPath);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        public static async Task EnsureTablesCreatedAsync()
        {
            try
            {
                using var conn = new NpgsqlConnection(CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                string ddl = @"
                CREATE TABLE IF NOT EXISTS users (
                    id SERIAL PRIMARY KEY,
                    username VARCHAR(50) UNIQUE NOT NULL,
                    email VARCHAR(255) UNIQUE NOT NULL,
                    password_hash VARCHAR(256) NOT NULL,
                    password_salt VARCHAR(64) NOT NULL,
                    full_name VARCHAR(100),
                    role VARCHAR(20) DEFAULT 'user',
                    avatar_url TEXT,
                    bio TEXT,
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                    last_login TIMESTAMP WITH TIME ZONE
                );
                CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
                CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
                ALTER TABLE users ADD COLUMN IF NOT EXISTS ban BOOLEAN DEFAULT FALSE;
                ";
                using var cmd = new NpgsqlCommand(ddl, conn);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EnsureTablesCreated error: " + ex.Message);
            }
        }

        public static async Task RefreshCurrentUserAsync()
        {
            if (CurrentUser == null) return;
            try
            {
                using var conn = new NpgsqlConnection(CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                string sql = "SELECT id, username, email, full_name, role, avatar_url, bio, created_at, last_login, COALESCE(ban, FALSE) as ban FROM users WHERE id = @id LIMIT 1;";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", CurrentUser.Id);

                using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                if (await reader.ReadAsync().ConfigureAwait(false))
                {
                    CurrentUser.Username = reader.GetString(1);
                    CurrentUser.Email = reader.GetString(2);
                    CurrentUser.FullName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    CurrentUser.Role = reader.IsDBNull(4) ? "user" : reader.GetString(4);
                    CurrentUser.AvatarUrl = reader.IsDBNull(5) ? "" : reader.GetString(5);
                    CurrentUser.Bio = reader.IsDBNull(6) ? "" : reader.GetString(6);
                    CurrentUser.CreatedAt = reader.IsDBNull(7) ? DateTime.UtcNow : reader.GetDateTime(7);
                    CurrentUser.LastLogin = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8);
                    CurrentUser.Ban = !reader.IsDBNull(9) && reader.GetBoolean(9);

                    SaveSession(CurrentUser);
                    OnAuthStateChanged?.Invoke();
                }
            }
            catch { }
        }

        public static async Task<(bool Success, string Error, UserProfile User)> RegisterAsync(
            string username, string email, string password, string fullName, string requestedRole = "user", bool rememberMe = true)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3)
                return (false, AppL10n.T("Tên đăng nhập phải có ít nhất 3 ký tự.", "Username must be at least 3 characters."), null);

            username = username.Trim();
            if (username.Length > 30)
                return (false, AppL10n.T("Tên đăng nhập không được dài quá 30 ký tự.", "Username must not exceed 30 characters."), null);

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || !email.Contains("."))
                return (false, AppL10n.T("Email không hợp lệ.", "Invalid email address."), null);

            email = email.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(password) || password.Length < 6)
                return (false, AppL10n.T("Mật khẩu phải có độ dài tối thiểu 6 ký tự.", "Password must be at least 6 characters."), null);

            fullName = string.IsNullOrWhiteSpace(fullName) ? username : fullName.Trim();

            // Tất cả tài khoản tạo trên ứng dụng luôn có vai trò mặc định là "user" (thường).
            // Quyền Creator hoặc Admin chỉ được cấp khi người quản trị chủ động chỉnh sửa trong CSDL PostgreSQL.
            string role = "user";

            try
            {
                using var conn = new NpgsqlConnection(CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                // 1. Kiểm tra username hoặc email đã tồn tại chưa
                string checkSql = "SELECT COUNT(*) FROM users WHERE LOWER(username) = LOWER(@u) OR LOWER(email) = LOWER(@e);";
                using (var checkCmd = new NpgsqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("u", username);
                    checkCmd.Parameters.AddWithValue("e", email);
                    long existsCount = Convert.ToInt64(await checkCmd.ExecuteScalarAsync().ConfigureAwait(false));
                    if (existsCount > 0)
                    {
                        return (false, AppL10n.T("Tên đăng nhập hoặc Email này đã được đăng ký trong hệ thống.", "This username or email is already registered."), null);
                    }
                }

                // 2. Tạo Salt và băm mật khẩu bằng PBKDF2 (100,000 vòng lặp an toàn tuyệt đối)
                byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
                string saltHex = Convert.ToHexString(saltBytes);
                string hashHex = HashPassword(password, saltBytes);

                // 4. Thêm người dùng mới vào PostgreSQL
                string insertSql = @"
                INSERT INTO users (username, email, password_hash, password_salt, full_name, role, created_at, last_login)
                VALUES (@u, @e, @h, @s, @fn, @r, NOW(), NOW())
                RETURNING id, created_at;
                ";
                int newId = 0;
                DateTime createdAt = DateTime.UtcNow;

                using (var insertCmd = new NpgsqlCommand(insertSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("u", username);
                    insertCmd.Parameters.AddWithValue("e", email);
                    insertCmd.Parameters.AddWithValue("h", hashHex);
                    insertCmd.Parameters.AddWithValue("s", saltHex);
                    insertCmd.Parameters.AddWithValue("fn", fullName);
                    insertCmd.Parameters.AddWithValue("r", role);

                    using var reader = await insertCmd.ExecuteReaderAsync().ConfigureAwait(false);
                    if (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        newId = reader.GetInt32(0);
                        createdAt = reader.GetDateTime(1);
                    }
                }

                var profile = new UserProfile
                {
                    Id = newId,
                    Username = username,
                    Email = email,
                    FullName = fullName,
                    Role = role,
                    CreatedAt = createdAt,
                    LastLogin = DateTime.UtcNow
                };

                CurrentUser = profile;
                if (rememberMe) SaveSession(profile);
                else SaveSession(null);
                OnAuthStateChanged?.Invoke();

                return (true, "", profile);
            }
            catch (Exception ex)
            {
                return (false, AppL10n.T("Lỗi kết nối cơ sở dữ liệu Aiven: ", "Database connection error: ") + ex.Message, null);
            }
        }

        public static async Task<(bool Success, string Error, UserProfile User)> LoginAsync(string usernameOrEmail, string password, bool rememberMe = true)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail))
                return (false, AppL10n.T("Vui lòng nhập tên đăng nhập hoặc email.", "Please enter your username or email."), null);

            if (string.IsNullOrEmpty(password))
                return (false, AppL10n.T("Vui lòng nhập mật khẩu.", "Please enter your password."), null);

            usernameOrEmail = usernameOrEmail.Trim();

            try
            {
                using var conn = new NpgsqlConnection(CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);

                string query = @"
                SELECT id, username, email, password_hash, password_salt, full_name, role, avatar_url, bio, created_at, COALESCE(ban, FALSE) as ban
                FROM users
                WHERE LOWER(username) = LOWER(@id) OR LOWER(email) = LOWER(@id)
                LIMIT 1;
                ";

                int id = 0;
                string u = "", e = "", hash = "", salt = "", fn = "", r = "user", av = "", bio = "";
                DateTime cr = DateTime.UtcNow;
                bool isBanned = false;

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("id", usernameOrEmail);
                    using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                    if (!await reader.ReadAsync().ConfigureAwait(false))
                    {
                        return (false, AppL10n.T("Tài khoản không tồn tại. Vui lòng kiểm tra lại tên đăng nhập hoặc email.", "Account not found. Please check your username or email."), null);
                    }

                    id = reader.GetInt32(0);
                    u = reader.GetString(1);
                    e = reader.GetString(2);
                    hash = reader.GetString(3);
                    salt = reader.GetString(4);
                    fn = reader.IsDBNull(5) ? u : reader.GetString(5);
                    r = reader.IsDBNull(6) ? "user" : reader.GetString(6);
                    av = reader.IsDBNull(7) ? "" : reader.GetString(7);
                    bio = reader.IsDBNull(8) ? "" : reader.GetString(8);
                    cr = reader.IsDBNull(9) ? DateTime.UtcNow : reader.GetDateTime(9);
                    isBanned = !reader.IsDBNull(10) && reader.GetBoolean(10);
                }

                // Kiểm tra khớp mật khẩu
                byte[] saltBytes = Convert.FromHexString(salt);
                string computedHash = HashPassword(password, saltBytes);
                if (!string.Equals(hash, computedHash, StringComparison.OrdinalIgnoreCase))
                {
                    // Hỗ trợ trường hợp người dùng copy paste hoặc gõ dính khoảng trắng đầu/cuối
                    string trimmedHash = HashPassword(password.Trim(), saltBytes);
                    if (!string.Equals(hash, trimmedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, AppL10n.T("Mật khẩu không chính xác. Vui lòng thử lại.", "Incorrect password. Please try again."), null);
                    }
                }

                // Cập nhật last_login
                try
                {
                    using var updateCmd = new NpgsqlCommand("UPDATE users SET last_login = NOW() WHERE id = @id;", conn);
                    updateCmd.Parameters.AddWithValue("id", id);
                    await updateCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                catch { }

                var user = new UserProfile
                {
                    Id = id,
                    Username = u,
                    Email = e,
                    FullName = fn,
                    Role = r,
                    AvatarUrl = av,
                    Bio = bio,
                    Ban = isBanned,
                    CreatedAt = cr,
                    LastLogin = DateTime.UtcNow
                };

                CurrentUser = user;
                if (rememberMe) SaveSession(user);
                else SaveSession(null);
                OnAuthStateChanged?.Invoke();

                return (true, "", user);
            }
            catch (Exception ex)
            {
                return (false, AppL10n.T("Lỗi kết nối cơ sở dữ liệu Aiven: ", "Database connection error: ") + ex.Message, null);
            }
        }

        public static void SignOut()
        {
            CurrentUser = null;
            SaveSession(null);
            OnAuthStateChanged?.Invoke();
        }

        public static async Task<System.Collections.Generic.HashSet<int>> GetBannedUserIdsAsync()
        {
            var set = new System.Collections.Generic.HashSet<int>();
            try
            {
                using var conn = new NpgsqlConnection(CONNECTION_STRING);
                await conn.OpenAsync().ConfigureAwait(false);
                using var cmd = new NpgsqlCommand("SELECT id FROM users WHERE ban = TRUE;", conn);
                using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    if (!reader.IsDBNull(0))
                    {
                        set.Add(reader.GetInt32(0));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetBannedUserIdsAsync error: " + ex.Message);
            }
            return set;
        }

        private static string HashPassword(string password, byte[] salt)
        {
            byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                new UTF8Encoding(false).GetBytes(password),
                salt,
                100000,
                HashAlgorithmName.SHA256,
                32
            );
            return Convert.ToHexString(hashBytes);
        }
    }
}

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZenQOffice.Core
{
    public class FeedbackPayload
    {
        public string Category { get; set; } = "Báo lỗi";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Contact { get; set; } = "";
        public string DeviceInfo { get; set; } = "";
        public string ImageUrl { get; set; } = "";
    }

    public class FeedbackResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public FeedbackResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    /// <summary>
    /// Dịch vụ gửi Báo lỗi & Đóng góp ý kiến trực tiếp lên Google Apps Script Webhook.
    /// Hoạt động bất đồng bộ (async), không làm đơ giao diện, tự động thu thập cấu hình thiết bị.
    /// </summary>
    public static class FeedbackService
    {
        public const string DefaultWebhookUrl = "https://script.google.com/macros/s/AKfycbwFOUzEf14JkfAqsuYkScjgRpbzD7M3JghMpyZpjm9uL6z3XjLa0pm20XgsnW3L6Man/exec";

        /// <summary>
        /// Tạo chuỗi tóm tắt thông tin phần cứng & hệ điều hành của máy người dùng.
        /// </summary>
        public static string GetDeviceInfoSummary()
        {
            try
            {
                string osName = "Windows";
                int build = Environment.OSVersion.Version.Build;
                if (build >= 22000) osName = $"Windows 11 (Build {build})";
                else if (build >= 10240) osName = $"Windows 10 (Build {build})";
                else osName = $"{Environment.OSVersion.VersionString}";

                string arch = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

                long ramBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                double ramGb = ramBytes > 0 ? Math.Round((double)ramBytes / (1024 * 1024 * 1024), 1) : 0;
                string ramStr = ramGb > 0 ? $"{ramGb} GB" : "N/A";

                var screen = Screen.PrimaryScreen?.Bounds ?? System.Drawing.Rectangle.Empty;
                string screenStr = screen.Width > 0 ? $"{screen.Width}x{screen.Height}" : "N/A";

                string appVer = Application.ProductVersion;
                if (string.IsNullOrWhiteSpace(appVer)) appVer = "4.0";

                return $"ZenQ Office v{appVer} | {osName} {arch} | RAM: {ramStr} | Screen: {screenStr} | .NET {Environment.Version}";
            }
            catch
            {
                return "ZenQ Office 6.0 | Windows";
            }
        }

        /// <summary>
        /// Gửi gói tin báo lỗi / góp ý bất đồng bộ lên Google Apps Script Webhook.
        /// </summary>
        public static async Task<FeedbackResult> SendFeedbackAsync(FeedbackPayload payload, string customWebhookUrl = null)
        {
            if (payload == null)
            {
                return new FeedbackResult(false, "Dữ liệu phản hồi trống.");
            }

            string targetUrl = !string.IsNullOrWhiteSpace(customWebhookUrl) 
                ? customWebhookUrl 
                : (!string.IsNullOrWhiteSpace(AppSettings.Instance.FeedbackWebhookUrl) ? AppSettings.Instance.FeedbackWebhookUrl : DefaultWebhookUrl);

            try
            {
                using var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 5
                };

                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };

                string fullTitle = string.IsNullOrWhiteSpace(payload.Category) 
                    ? payload.Title 
                    : $"[{payload.Category}] {payload.Title}";

                var bodyObj = new
                {
                    userId = string.IsNullOrWhiteSpace(payload.Contact) ? "Ẩn danh" : payload.Contact.Trim(),
                    title = fullTitle.Trim(),
                    description = (payload.Description ?? "").Trim(),
                    deviceInfo = (payload.DeviceInfo ?? "").Trim(),
                    imageUrl = (payload.ImageUrl ?? "").Trim()
                };

                string json = System.Text.Json.JsonSerializer.Serialize(bodyObj);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(targetUrl, content).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return new FeedbackResult(true, "Báo lỗi đã được tiếp nhận thành công vào hệ thống!");
                }
                else
                {
                    return new FeedbackResult(false, $"Máy chủ phản hồi mã {(int)response.StatusCode}: {response.ReasonPhrase}");
                }
            }
            catch (TaskCanceledException)
            {
                return new FeedbackResult(false, "Quá thời gian chờ phản hồi (Timeout). Vui lòng kiểm tra lại kết nối mạng.");
            }
            catch (Exception ex)
            {
                return new FeedbackResult(false, $"Lỗi kết nối: {ex.Message}");
            }
        }
    }
}

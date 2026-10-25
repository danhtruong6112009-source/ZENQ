using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZenQOffice.Core
{
    /// <summary>
    /// Kết quả tìm kiếm đơn lẻ từ Internet
    /// </summary>
    public class WebSearchResultItem
    {
        public string Title { get; set; } = "";
        public string Snippet { get; set; } = "";
        public string Url { get; set; } = "";
    }

    /// <summary>
    /// Dịch vụ Tìm kiếm Internet Thời gian thực cho AI Agent (Bing & DuckDuckGo Search)
    /// Tự động tra cứu ngữ liệu sách giáo khoa, từ vựng, ngữ pháp và thông tin thời sự mới nhất.
    /// </summary>
    public static class AiWebSearchService
    {
        private static readonly HttpClient _httpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(7)
            };
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept-Language", "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");
            return client;
        }

        /// <summary>
        /// Kiểm tra xem truy vấn của người dùng có cần tìm kiếm trên Internet hay không.
        /// </summary>
        public static bool ShouldSearchWeb(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return false;
            string q = query.ToLowerInvariant();

            // Nếu người dùng ra lệnh /canvas, luôn tìm kiếm ngữ liệu chính xác
            if (q.StartsWith("/canvas") || q.Contains("/canvas")) return true;

            // Các từ khóa giáo trình, sách giáo khoa, chương trình giáo dục
            string[] educationKeywords = new[]
            {
                "unit", "global success", "sgk", "sách giáo khoa", "sách bài tập", "sbt",
                "kết nối tri thức", "chân trời sáng tạo", "cánh diều",
                "tiếng anh", "ngữ văn", "lịch sử", "địa lý", "vật lý", "hóa học", "sinh học", "toán",
                "tìm trên mạng", "tra cứu", "tìm kiếm", "thời sự", "tin tức", "mới nhất", "hiện tại",
                "ngữ liệu", "đọc hiểu", "bài tập", "đề thi", "kiểm tra", "ôn tập"
            };

            foreach (var kw in educationKeywords)
            {
                if (q.Contains(kw)) return true;
            }

            return false;
        }

        /// <summary>
        /// Trích xuất từ khóa tìm kiếm tối ưu từ câu hỏi người dùng
        /// </summary>
        public static string ExtractSearchQuery(string userText)
        {
            if (string.IsNullOrWhiteSpace(userText)) return "";
            string q = userText;

            // Bỏ tiền tố lệnh /canvas, /quiz, /search
            q = Regex.Replace(q, @"^/(?:canvas|quiz|search|find|web)\s*", "", RegexOptions.IgnoreCase);

            // Bỏ các từ đệm thông thường
            q = Regex.Replace(q, @"\b(?:tạo|làm|hãy|cho tôi|giúp tôi|soạn|viết|đề|câu hỏi|bài tập)\b", "", RegexOptions.IgnoreCase);
            q = Regex.Replace(q, @"\s+", " ").Trim();

            if (q.Length > 120) q = q.Substring(0, 120).Trim();
            return string.IsNullOrWhiteSpace(q) ? userText.Trim() : q;
        }

        /// <summary>
        /// Thực hiện tìm kiếm web bất đồng bộ và trả về danh sách kết quả
        /// </summary>
        public static async Task<List<WebSearchResultItem>> SearchAsync(string rawQuery)
        {
            var results = new List<WebSearchResultItem>();
            string cleanQ = ExtractSearchQuery(rawQuery);
            if (string.IsNullOrWhiteSpace(cleanQ)) return results;

            // 1. Thử tìm kiếm qua Bing
            try
            {
                string searchUrl = "https://www.bing.com/search?q=" + Uri.EscapeDataString(cleanQ);
                string html = await _httpClient.GetStringAsync(searchUrl).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(html))
                {
                    // Lọc các thẻ <li class="b_algo">
                    var algoMatches = Regex.Matches(html, @"<li class=""b_algo""[^>]*>(.*?)</li>", RegexOptions.Singleline);
                    foreach (Match m in algoMatches)
                    {
                        string block = m.Groups[1].Value;

                        // Tiêu đề
                        var tMatch = Regex.Match(block, @"<h2><a[^>]*>(.*?)</a></h2>", RegexOptions.Singleline);
                        string title = tMatch.Success ? StripHtml(tMatch.Groups[1].Value) : "";

                        // Đoạn trích
                        var sMatch = Regex.Match(block, @"<p[^>]*>(.*?)</p>", RegexOptions.Singleline);
                        string snippet = sMatch.Success ? StripHtml(sMatch.Groups[1].Value) : "";

                        // URL
                        var uMatch = Regex.Match(block, @"<h2><a\s+[^>]*href=""([^""]+)""", RegexOptions.Singleline);
                        string url = uMatch.Success ? uMatch.Groups[1].Value : "";

                        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(snippet))
                        {
                            results.Add(new WebSearchResultItem
                            {
                                Title = WebUtility.HtmlDecode(title).Trim(),
                                Snippet = WebUtility.HtmlDecode(snippet).Trim(),
                                Url = url
                            });
                        }

                        if (results.Count >= 5) break;
                    }
                }
            }
            catch { }

            // 2. Dự phòng: Nếu Bing không có kết quả, thử qua DuckDuckGo Lite
            if (results.Count == 0)
            {
                try
                {
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("q", cleanQ)
                    });
                    var resp = await _httpClient.PostAsync("https://lite.duckduckgo.com/lite/", formContent).ConfigureAwait(false);
                    if (resp.IsSuccessStatusCode)
                    {
                        string html = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var linkMatches = Regex.Matches(html, @"<a\s+class=""result-link""[^>]*>(.*?)</a>", RegexOptions.Singleline);
                        var snipMatches = Regex.Matches(html, @"<td\s+class=""result-snippet""[^>]*>(.*?)</td>", RegexOptions.Singleline);

                        int count = Math.Min(linkMatches.Count, snipMatches.Count);
                        for (int i = 0; i < count && results.Count < 4; i++)
                        {
                            string title = StripHtml(linkMatches[i].Groups[1].Value);
                            string snip = StripHtml(snipMatches[i].Groups[1].Value);
                            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(snip))
                            {
                                results.Add(new WebSearchResultItem
                                {
                                    Title = WebUtility.HtmlDecode(title).Trim(),
                                    Snippet = WebUtility.HtmlDecode(snip).Trim()
                                });
                            }
                        }
                    }
                }
                catch { }
            }

            return results;
        }

        /// <summary>
        /// Tạo ngữ cảnh tìm kiếm định dạng Markdown để chèn trực tiếp vào Prompt cho AI
        /// </summary>
        public static string FormatResultsForPrompt(string query, List<WebSearchResultItem> results)
        {
            if (results == null || results.Count == 0) return "";

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("======================================================================");
            sb.AppendLine("[THÔNG TIN TRA CỨU TỪ INTERNET THỜI GIAN THỰC (AGENT WEB SEARCH)]");
            sb.AppendLine("Chủ đề tìm kiếm: " + query);
            sb.AppendLine("----------------------------------------------------------------------");

            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                sb.AppendLine(string.Format("{0}. Tiêu đề: {1}", i + 1, item.Title));
                sb.AppendLine("   Nội dung: " + item.Snippet);
                sb.AppendLine();
            }

            sb.AppendLine("QUY TẮC BẮT BUỘC VỀ NỘI DUNG VÀ TÍNH CHÍNH XÁC:");
            sb.AppendLine("1. Hãy đối chiếu và sử dụng CHÍNH XÁC các ngữ liệu, kiến thức, từ vựng và ngữ pháp từ các kết quả tìm kiếm trên.");
            sb.AppendLine("2. Tuyệt đối KHÔNG tự ý suy diễn hoặc thay đổi sang chủ đề khác (Ví dụ: nếu đề bài là Unit 2 Tiếng Anh 12 'A Multicultural World', phải dùng đúng từ vựng về văn hóa/đa dạng văn hóa/mạo từ, KHÔNG được làm nhầm sang Generation Gap của lớp 11).");
            sb.AppendLine("3. Nếu người dùng yêu cầu 'ngữ liệu' hoặc 'bài đọc', hãy đưa đoạn trích ngữ liệu và câu hỏi hoặc từ vựng tương ứng vào Canvas theo đúng chủ đề.");
            sb.AppendLine("======================================================================");
            sb.AppendLine();

            return sb.ToString();
        }

        private static string StripHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            string s = Regex.Replace(input, @"<[^>]+>", " ");
            s = Regex.Replace(s, @"\s+", " ");
            return s.Trim();
        }
    }
}

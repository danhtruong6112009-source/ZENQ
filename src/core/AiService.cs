using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace ZenQOffice.Core
{
    /// <summary>
    /// Bản ghi tin nhắn trong lịch sử hội thoại nhiều lượt (Multi-turn Chat).
    /// </summary>
    public class AiChatMessage
    {
        public string Role { get; set; } // "user" hoặc "assistant" / "model"
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }

        public AiChatMessage()
        {
            Role = "user";
            Content = "";
            Timestamp = DateTime.Now;
        }

        public AiChatMessage(string role, string content)
        {
            Role = role;
            Content = content ?? "";
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Kết quả trả về từ yêu cầu AI Copilot.
    /// </summary>
    public class AiResponse
    {
        public bool Success { get; set; }
        public string Text { get; set; }
        public string ErrorMessage { get; set; }
        public long DurationMs { get; set; }
        public string ModelUsed { get; set; }

        public AiResponse()
        {
            Success = false;
            Text = "";
            ErrorMessage = "";
            DurationMs = 0;
            ModelUsed = "";
        }
    }

    /// <summary>
    /// Dịch vụ kết nối và giao tiếp AI đa nền tảng (Google AI Studio, Groq, OpenAI, DeepSeek, Ollama, OpenRouter, Custom).
    /// Hỗ trợ bảo mật TLS 1.2+, xử lý luồng nền không gây đơ giao diện người dùng, nhớ lịch sử đối thoại.
    /// </summary>
    public static class AiService
    {
        static AiService()
        {
            try
            {
                // Bật TLS 1.2 (3072) và TLS 1.1 (768) trên .NET Framework 4.0/4.5
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | SecurityProtocolType.Tls;
                ServicePointManager.DefaultConnectionLimit = 32;
                ServicePointManager.Expect100Continue = false;
            }
            catch { }
        }

        /// <summary>
        /// Kiểm tra kết nối nhanh tới Model và API Key của nhà cung cấp đã chọn.
        /// </summary>
        public static AiResponse TestConnection(AiConfig config)
        {
            Stopwatch sw = Stopwatch.StartNew();
            AiResponse resp = new AiResponse();
            resp.ModelUsed = config.ModelName;

            if (config.Provider != AiProviderType.Ollama && string.IsNullOrWhiteSpace(config.ApiKey))
            {
                resp.Success = false;
                resp.ErrorMessage = "Vui lòng nhập API Key trước khi kiểm tra.";
                sw.Stop();
                resp.DurationMs = sw.ElapsedMilliseconds;
                return resp;
            }

            if (string.IsNullOrWhiteSpace(config.ModelName))
            {
                resp.Success = false;
                resp.ErrorMessage = "Vui lòng nhập tên Model cần kiểm tra.";
                sw.Stop();
                resp.DurationMs = sw.ElapsedMilliseconds;
                return resp;
            }

            if (config.Provider == AiProviderType.Ollama)
            {
                var localModels = ScanLocalOllamaModels();
                if (localModels != null && localModels.Count > 0)
                {
                    sw.Stop();
                    resp.Success = true;
                    resp.DurationMs = sw.ElapsedMilliseconds;
                    resp.Text = string.Format("Ollama Local đang hoạt động tốt với {0} model ({1}).", localModels.Count, string.Join(", ", localModels.ToArray()));
                    return resp;
                }
                else
                {
                    sw.Stop();
                    resp.Success = false;
                    resp.ErrorMessage = "Không thể kết nối tới Ollama daemon. Hãy kiểm tra 'ollama serve' tại http://localhost:11434.";
                    resp.DurationMs = sw.ElapsedMilliseconds;
                    return resp;
                }
            }

            try
            {
                string testPrompt = "Chào bạn! Hãy trả lời ngắn đúng 1 câu: 'ZENQ AI kết nối thành công!'";
                resp = SendPrompt(config, "Bạn là trợ lý kiểm tra kết nối hệ thống.", testPrompt, "");
                sw.Stop();
                resp.DurationMs = sw.ElapsedMilliseconds;
                return resp;
            }
            catch (Exception ex)
            {
                sw.Stop();
                resp.Success = false;
                resp.ErrorMessage = "Lỗi kết nối: " + ex.Message;
                resp.DurationMs = sw.ElapsedMilliseconds;
                return resp;
            }
        }

        /// <summary>
        /// Gửi yêu cầu Prompt tới AI (Đồng bộ) kèm ngữ cảnh và lịch sử hội thoại nhiều lượt.
        /// </summary>
        public static AiResponse SendPrompt(AiConfig config, string systemPrompt, string userPrompt, string contextData = "", List<AiChatMessage> history = null)
        {
            Stopwatch sw = Stopwatch.StartNew();
            AiResponse resp = new AiResponse();
            resp.ModelUsed = config.ModelName;

            try
            {
                string fullSystemPrompt = (config.SystemInstructions ?? "") + "\n" + (systemPrompt ?? "");
                string fullUserPrompt = userPrompt ?? "";

                if (!string.IsNullOrWhiteSpace(contextData))
                {
                    fullUserPrompt = "[NGỮ CẢNH TÀI LIỆU HIỆN TẠI]:\n" + contextData + "\n\n[YÊU CẦU CỦA NGƯỜI DÙNG]:\n" + fullUserPrompt;
                }

                if (config.Provider == AiProviderType.GoogleAIStudio)
                {
                    resp = CallGoogleGemini(config, fullSystemPrompt, fullUserPrompt, history);
                }
                else
                {
                    resp = CallOpenAiCompatible(config, fullSystemPrompt, fullUserPrompt, history);
                }
            }
            catch (WebException wex)
            {
                resp.Success = false;
                string errorDetail = "";
                int statusCode = 0;
                try
                {
                    if (wex.Response != null)
                    {
                        if (wex.Response is HttpWebResponse)
                        {
                            statusCode = (int)((HttpWebResponse)wex.Response).StatusCode;
                        }
                        using (StreamReader reader = new StreamReader(wex.Response.GetResponseStream(), Encoding.UTF8))
                        {
                            errorDetail = reader.ReadToEnd();
                        }
                    }
                }
                catch { }

                resp.ErrorMessage = FormatAiErrorMessage(errorDetail, statusCode, config.ModelName, wex);
            }
            catch (Exception ex)
            {
                resp.Success = false;
                resp.ErrorMessage = FormatAiErrorMessage("", 0, config.ModelName, ex);
            }
            finally
            {
                sw.Stop();
                resp.DurationMs = sw.ElapsedMilliseconds;
            }

            return resp;
        }

        /// <summary>
        /// Gửi yêu cầu Prompt tới AI bất đồng bộ (Asynchronous) trên luồng nền ThreadPool kèm lịch sử hội thoại.
        /// </summary>
        public static void SendPromptAsync(AiConfig config, string userPrompt, Action<AiResponse> onCompleted, List<AiChatMessage> history = null)
        {
            SendPromptAsync(config, config != null ? config.SystemInstructions : "", userPrompt, "", onCompleted, history);
        }

        public static void SendPromptAsync(AiConfig config, string systemPrompt, string userPrompt, string contextData, Action<AiResponse> onCompleted, List<AiChatMessage> history = null)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                AiResponse response = SendPrompt(config, systemPrompt, userPrompt, contextData, history);
                if (onCompleted != null)
                {
                    try { onCompleted(response); } catch { }
                }
            });
        }

        public static void SendPromptStreamAsync(AiConfig config, string userPrompt, Action<string> onChunk, Action<AiResponse> onCompleted, List<AiChatMessage> history = null, Func<bool> isCancelled = null)
        {
            SendPromptStreamAsync(config, config != null ? config.SystemInstructions : "", userPrompt, "", onChunk, onCompleted, history, isCancelled);
        }

        public static void SendPromptStreamAsync(AiConfig config, string systemPrompt, string userPrompt, string contextData, Action<string> onChunk, Action<AiResponse> onCompleted, List<AiChatMessage> history = null, Func<bool> isCancelled = null)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                AiResponse response = SendPromptStream(config, systemPrompt, userPrompt, contextData, onChunk, history, isCancelled);
                if (onCompleted != null)
                {
                    try { onCompleted(response); } catch { }
                }
            });
        }

        public static AiResponse SendPromptStream(AiConfig config, string systemPrompt, string userPrompt, string contextData = "", Action<string> onChunk = null, List<AiChatMessage> history = null, Func<bool> isCancelled = null)
        {
            Stopwatch sw = Stopwatch.StartNew();
            AiResponse resp = new AiResponse();
            resp.ModelUsed = config.ModelName;

            try
            {
                string fullSystemPrompt = systemPrompt ?? "";
                if (string.IsNullOrWhiteSpace(fullSystemPrompt))
                {
                    fullSystemPrompt = config.SystemInstructions ?? "";
                }
                
                string fullUserPrompt = userPrompt ?? "";

                if (!string.IsNullOrWhiteSpace(contextData))
                {
                    fullUserPrompt = "[NGỮ CẢNH TÀI LIỆU HIỆN TẠI]:\n" + contextData + "\n\n[YÊU CẦU CỦA NGƯỜI DÙNG]:\n" + fullUserPrompt;
                }

                if (config.Provider == AiProviderType.GoogleAIStudio)
                {
                    resp = CallGoogleGeminiStream(config, fullSystemPrompt, fullUserPrompt, onChunk, history, isCancelled);
                }
                else
                {
                    resp = CallOpenAiCompatibleStream(config, fullSystemPrompt, fullUserPrompt, onChunk, history, isCancelled);
                }
            }
            catch (WebException wex)
            {
                resp.Success = false;
                string errorDetail = "";
                int statusCode = 0;
                try
                {
                    if (wex.Response != null)
                    {
                        if (wex.Response is HttpWebResponse)
                        {
                            statusCode = (int)((HttpWebResponse)wex.Response).StatusCode;
                        }
                        using (StreamReader reader = new StreamReader(wex.Response.GetResponseStream(), Encoding.UTF8))
                        {
                            errorDetail = reader.ReadToEnd();
                        }
                    }
                }
                catch { }

                resp.ErrorMessage = FormatAiErrorMessage(errorDetail, statusCode, config.ModelName, wex);
            }
            catch (Exception ex)
            {
                resp.Success = false;
                resp.ErrorMessage = FormatAiErrorMessage("", 0, config.ModelName, ex);
            }
            finally
            {
                sw.Stop();
                resp.DurationMs = sw.ElapsedMilliseconds;
            }

            return resp;
        }

        public static void TestConnectionAsync(AiConfig config, Action<bool, long, string> onCompleted)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                AiResponse resp = TestConnection(config);
                if (onCompleted != null)
                {
                    try
                    {
                        onCompleted(resp.Success, resp.DurationMs, resp.Success ? "Thành công" : resp.ErrorMessage);
                    }
                    catch { }
                }
            });
        }

        #region Google AI Studio (Gemini v1beta REST API)
        private static AiResponse CallGoogleGemini(AiConfig config, string systemPrompt, string userPrompt, List<AiChatMessage> history = null)
        {
            AiResponse result = new AiResponse();
            result.ModelUsed = config.ModelName;

            string endpoint = config.GetEffectiveEndpoint().TrimEnd('/');
            string model = string.IsNullOrWhiteSpace(config.ModelName) ? "gemini-3.7-flash" : config.ModelName.Trim();
            string apiKey = config.ApiKey != null ? config.ApiKey.Trim() : "";

            string url = string.Format("{0}/models/{1}:generateContent?key={2}", endpoint, model, apiKey);

            // Xây dựng JSON payload
            var payload = new Dictionary<string, object>();

            var contents = new List<object>();

            // Thêm lịch sử hội thoại các lượt trước
            if (history != null && history.Count > 0)
            {
                foreach (var h in history)
                {
                    if (string.IsNullOrWhiteSpace(h.Content)) continue;
                    var hContent = new Dictionary<string, object>();
                    hContent["role"] = (h.Role == "assistant" || h.Role == "model") ? "model" : "user";
                    var hParts = new List<object>();
                    var hPart = new Dictionary<string, object>();
                    hPart["text"] = h.Content;
                    hParts.Add(hPart);
                    hContent["parts"] = hParts;
                    contents.Add(hContent);
                }
            }

            // Thêm câu hỏi/yêu cầu hiện tại của người dùng
            var userContent = new Dictionary<string, object>();
            userContent["role"] = "user";
            var parts = new List<object>();
            var part = new Dictionary<string, object>();
            part["text"] = userPrompt;
            parts.Add(part);
            userContent["parts"] = parts;
            contents.Add(userContent);
            payload["contents"] = contents;

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                var sysContent = new Dictionary<string, object>();
                var sysParts = new List<object>();
                var sysPart = new Dictionary<string, object>();
                sysPart["text"] = systemPrompt;
                sysParts.Add(sysPart);
                sysContent["parts"] = sysParts;
                payload["systemInstruction"] = sysContent;
            }

            var genConfig = new Dictionary<string, object>();
            genConfig["temperature"] = (double)config.Temperature;
            payload["generationConfig"] = genConfig;

            string jsonBody = JsonLite.Serialize(payload);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";
            request.Timeout = 60000;
            request.ReadWriteTimeout = 60000;

            byte[] data = Encoding.UTF8.GetBytes(jsonBody);
            request.ContentLength = data.Length;
            using (Stream reqStream = request.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                string respJson = reader.ReadToEnd();
                var parsed = JsonLite.Parse(respJson) as Dictionary<string, object>;
                if (parsed != null)
                {
                    var candidates = JsonLite.GetArray(parsed, "candidates");
                    if (candidates != null && candidates.Count > 0)
                    {
                        var firstCand = candidates[0] as Dictionary<string, object>;
                        if (firstCand != null)
                        {
                            var contentObj = JsonLite.GetObject(firstCand, "content");
                            if (contentObj != null)
                            {
                                var candParts = JsonLite.GetArray(contentObj, "parts");
                                if (candParts != null && candParts.Count > 0)
                                {
                                    StringBuilder textBuilder = new StringBuilder();
                                    foreach (var pObj in candParts)
                                    {
                                        var pDict = pObj as Dictionary<string, object>;
                                        if (pDict != null && pDict.ContainsKey("text"))
                                        {
                                            textBuilder.Append(JsonLite.GetString(pDict, "text"));
                                        }
                                    }
                                    result.Success = true;
                                    result.Text = textBuilder.ToString();
                                    return result;
                                }
                            }
                        }
                    }
                }

                result.Success = false;
                result.ErrorMessage = "Phản hồi từ Google AI Studio không có dữ liệu văn bản hợp lệ.";
            }

            return result;
        }
        #endregion

        #region OpenAI Compatible (Groq, OpenAI, DeepSeek, Ollama, OpenRouter, Custom)
        private static AiResponse CallOpenAiCompatible(AiConfig config, string systemPrompt, string userPrompt, List<AiChatMessage> history = null)
        {
            AiResponse result = new AiResponse();
            result.ModelUsed = config.ModelName;

            string endpoint = config.GetEffectiveEndpoint().TrimEnd('/');
            string url = endpoint;
            if (!url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                url = url + "/chat/completions";
            }

            string model = string.IsNullOrWhiteSpace(config.ModelName) ? AiConfig.GetDefaultModel(config.Provider) : config.ModelName.Trim();
            string apiKey = config.ApiKey != null ? config.ApiKey.Trim() : "";

            var payload = new Dictionary<string, object>();
            payload["model"] = model;
            payload["temperature"] = (double)config.Temperature;
            payload["max_tokens"] = 16384;
            payload["frequency_penalty"] = 0.3;
            payload["presence_penalty"] = 0.3;

            if (config.Provider == AiProviderType.Ollama)
            {
                var options = new Dictionary<string, object>();
                options["temperature"] = (double)config.Temperature;
                options["num_ctx"] = 32768;
                options["num_predict"] = 16384;
                options["repeat_penalty"] = 1.18;
                options["top_p"] = 0.9;
                options["top_k"] = 40;
                payload["options"] = options;
            }

            var messages = new List<object>();

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                var sysMsg = new Dictionary<string, object>();
                sysMsg["role"] = "system";
                sysMsg["content"] = systemPrompt;
                messages.Add(sysMsg);
            }

            // Thêm lịch sử hội thoại các lượt trước
            if (history != null && history.Count > 0)
            {
                foreach (var h in history)
                {
                    if (string.IsNullOrWhiteSpace(h.Content)) continue;
                    var hMsg = new Dictionary<string, object>();
                    hMsg["role"] = (h.Role == "model") ? "assistant" : h.Role;
                    hMsg["content"] = h.Content;
                    messages.Add(hMsg);
                }
            }

            // Thêm câu hỏi/yêu cầu hiện tại của người dùng
            var userMsg = new Dictionary<string, object>();
            userMsg["role"] = "user";
            userMsg["content"] = userPrompt;
            messages.Add(userMsg);

            payload["messages"] = messages;

            string jsonBody = JsonLite.Serialize(payload);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";
            int timeoutMs = (config.Provider == AiProviderType.Ollama) ? 180000 : 60000;
            request.Timeout = timeoutMs;
            request.ReadWriteTimeout = timeoutMs;

            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers["Authorization"] = "Bearer " + apiKey;
            }

            // Headers phụ cho OpenRouter nếu cần
            if (config.Provider == AiProviderType.OpenRouter)
            {
                request.Headers["HTTP-Referer"] = "https://genoffice.app";
                request.Headers["X-Title"] = "GenOffice Suite";
            }

            byte[] data = Encoding.UTF8.GetBytes(jsonBody);
            request.ContentLength = data.Length;
            using (Stream reqStream = request.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                string respJson = reader.ReadToEnd();
                var parsed = JsonLite.Parse(respJson) as Dictionary<string, object>;
                if (parsed != null)
                {
                    var choices = JsonLite.GetArray(parsed, "choices");
                    if (choices != null && choices.Count > 0)
                    {
                        var firstChoice = choices[0] as Dictionary<string, object>;
                        if (firstChoice != null)
                        {
                            var msgObj = JsonLite.GetObject(firstChoice, "message");
                            if (msgObj != null)
                            {
                                string content = JsonLite.GetString(msgObj, "content");
                                result.Success = true;
                                result.Text = content;
                                return result;
                            }
                        }
                    }
                }

                result.Success = false;
                result.ErrorMessage = "Phản hồi từ API (" + config.Provider + ") không chứa dữ liệu choices hợp lệ.";
            }

            return result;
        }

        private static AiResponse CallGoogleGeminiStream(AiConfig config, string systemPrompt, string userPrompt, Action<string> onChunk, List<AiChatMessage> history = null, Func<bool> isCancelled = null)
        {
            AiResponse result = new AiResponse();
            result.ModelUsed = config.ModelName;

            string endpoint = config.GetEffectiveEndpoint().TrimEnd('/');
            string model = string.IsNullOrWhiteSpace(config.ModelName) ? "gemini-3.7-flash" : config.ModelName.Trim();
            string apiKey = config.ApiKey != null ? config.ApiKey.Trim() : "";

            string url = string.Format("{0}/models/{1}:streamGenerateContent?key={2}&alt=sse", endpoint, model, apiKey);

            var payload = new Dictionary<string, object>();
            var contents = new List<object>();

            if (history != null && history.Count > 0)
            {
                foreach (var h in history)
                {
                    if (string.IsNullOrWhiteSpace(h.Content)) continue;
                    var hContent = new Dictionary<string, object>();
                    hContent["role"] = (h.Role == "assistant" || h.Role == "model") ? "model" : "user";
                    var hParts = new List<object>();
                    var hPart = new Dictionary<string, object>();
                    hPart["text"] = h.Content;
                    hParts.Add(hPart);
                    hContent["parts"] = hParts;
                    contents.Add(hContent);
                }
            }

            var userContent = new Dictionary<string, object>();
            userContent["role"] = "user";
            var parts = new List<object>();
            var part = new Dictionary<string, object>();
            part["text"] = userPrompt;
            parts.Add(part);
            userContent["parts"] = parts;
            contents.Add(userContent);
            payload["contents"] = contents;

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                var sysContent = new Dictionary<string, object>();
                var sysParts = new List<object>();
                var sysPart = new Dictionary<string, object>();
                sysPart["text"] = systemPrompt;
                sysParts.Add(sysPart);
                sysContent["parts"] = sysParts;
                payload["systemInstruction"] = sysContent;
            }

            var genConfig = new Dictionary<string, object>();
            genConfig["temperature"] = (double)config.Temperature;
            payload["generationConfig"] = genConfig;

            string jsonBody = JsonLite.Serialize(payload);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";
            request.Timeout = 90000;
            request.ReadWriteTimeout = 90000;

            byte[] data = Encoding.UTF8.GetBytes(jsonBody);
            request.ContentLength = data.Length;
            using (Stream reqStream = request.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
            }

            StringBuilder fullTextBuilder = new StringBuilder();

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (isCancelled != null && isCancelled())
                    {
                        result.Success = true;
                        result.Text = fullTextBuilder.ToString();
                        return result;
                    }
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.StartsWith("data:"))
                    {
                        string rawJson = line.Substring(5).Trim();
                        var parsed = JsonLite.Parse(rawJson) as Dictionary<string, object>;
                        if (parsed != null)
                        {
                            var candidates = JsonLite.GetArray(parsed, "candidates");
                            if (candidates != null && candidates.Count > 0)
                            {
                                var firstCand = candidates[0] as Dictionary<string, object>;
                                if (firstCand != null)
                                {
                                    var contentObj = JsonLite.GetObject(firstCand, "content");
                                    if (contentObj != null)
                                    {
                                        var candParts = JsonLite.GetArray(contentObj, "parts");
                                        if (candParts != null && candParts.Count > 0)
                                        {
                                            foreach (var pObj in candParts)
                                            {
                                                var pDict = pObj as Dictionary<string, object>;
                                                if (pDict != null && pDict.ContainsKey("text"))
                                                {
                                                    string textChunk = JsonLite.GetString(pDict, "text");
                                                    if (!string.IsNullOrEmpty(textChunk))
                                                    {
                                                        fullTextBuilder.Append(textChunk);
                                                        if (onChunk != null)
                                                        {
                                                            try { onChunk(textChunk); } catch { }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            result.Success = true;
            result.Text = fullTextBuilder.ToString();
            return result;
        }

        private static AiResponse CallOpenAiCompatibleStream(AiConfig config, string systemPrompt, string userPrompt, Action<string> onChunk, List<AiChatMessage> history = null, Func<bool> isCancelled = null)
        {
            AiResponse result = new AiResponse();
            result.ModelUsed = config.ModelName;

            string endpoint = config.GetEffectiveEndpoint().TrimEnd('/');
            string url = endpoint;
            if (!url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                url = url + "/chat/completions";
            }

            string model = string.IsNullOrWhiteSpace(config.ModelName) ? AiConfig.GetDefaultModel(config.Provider) : config.ModelName.Trim();
            string apiKey = config.ApiKey != null ? config.ApiKey.Trim() : "";

            var payload = new Dictionary<string, object>();
            payload["model"] = model;
            payload["temperature"] = (double)config.Temperature;
            payload["stream"] = true; // Streaming token-by-token
            payload["max_tokens"] = 16384;
            payload["frequency_penalty"] = 0.3;
            payload["presence_penalty"] = 0.3;

            if (config.Provider == AiProviderType.Ollama)
            {
                var options = new Dictionary<string, object>();
                options["temperature"] = (double)config.Temperature;
                options["num_ctx"] = 32768;
                options["num_predict"] = 16384;
                options["repeat_penalty"] = 1.18;
                options["top_p"] = 0.9;
                options["top_k"] = 40;
                payload["options"] = options;
            }

            var messages = new List<object>();

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                var sysMsg = new Dictionary<string, object>();
                sysMsg["role"] = "system";
                sysMsg["content"] = systemPrompt;
                messages.Add(sysMsg);
            }

            if (history != null && history.Count > 0)
            {
                foreach (var h in history)
                {
                    if (string.IsNullOrWhiteSpace(h.Content)) continue;
                    var hMsg = new Dictionary<string, object>();
                    hMsg["role"] = (h.Role == "model") ? "assistant" : h.Role;
                    hMsg["content"] = h.Content;
                    messages.Add(hMsg);
                }
            }

            var userMsg = new Dictionary<string, object>();
            userMsg["role"] = "user";
            userMsg["content"] = userPrompt;
            messages.Add(userMsg);

            payload["messages"] = messages;

            string jsonBody = JsonLite.Serialize(payload);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";
            int timeoutMs = (config.Provider == AiProviderType.Ollama) ? 300000 : 90000;
            request.Timeout = timeoutMs;
            request.ReadWriteTimeout = timeoutMs;

            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers["Authorization"] = "Bearer " + apiKey;
            }

            if (config.Provider == AiProviderType.OpenRouter)
            {
                request.Headers["HTTP-Referer"] = "https://genoffice.app";
                request.Headers["X-Title"] = "GenOffice Suite";
            }

            byte[] data = Encoding.UTF8.GetBytes(jsonBody);
            request.ContentLength = data.Length;
            using (Stream reqStream = request.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
            }

            StringBuilder fullTextBuilder = new StringBuilder();

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (isCancelled != null && isCancelled())
                    {
                        result.Success = true;
                        result.Text = fullTextBuilder.ToString();
                        return result;
                    }
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.StartsWith("data:"))
                    {
                        string rawJson = line.Substring(5).Trim();
                        if (rawJson == "[DONE]") break;

                        var parsed = JsonLite.Parse(rawJson) as Dictionary<string, object>;
                        if (parsed != null)
                        {
                            var choices = JsonLite.GetArray(parsed, "choices");
                            if (choices != null && choices.Count > 0)
                            {
                                var firstChoice = choices[0] as Dictionary<string, object>;
                                if (firstChoice != null)
                                {
                                    var delta = JsonLite.GetObject(firstChoice, "delta");
                                    if (delta != null && delta.ContainsKey("content"))
                                    {
                                        string chunk = JsonLite.GetString(delta, "content");
                                        if (!string.IsNullOrEmpty(chunk))
                                        {
                                            fullTextBuilder.Append(chunk);
                                            if (onChunk != null)
                                            {
                                                try { onChunk(chunk); } catch { }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            result.Success = true;
            result.Text = fullTextBuilder.ToString();
            return result;
        }
        #endregion

        #region Ollama Model Scanner
        /// <summary>
        /// Quét các model Ollama cục bộ đang có sẵn trên máy (qua REST API /api/tags hoặc CLI ollama list).
        /// </summary>
        public static List<string> ScanLocalOllamaModels(string endpoint = null)
        {
            List<string> models = new List<string>();

            // 1. Thử gọi REST API endpoint http://localhost:11434/api/tags
            string baseHost = !string.IsNullOrWhiteSpace(endpoint) ? endpoint.TrimEnd('/') : "http://localhost:11434";
            if (baseHost.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                baseHost = baseHost.Substring(0, baseHost.Length - 3);
            }
            string tagsUrl = baseHost + "/api/tags";

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(tagsUrl);
                request.Method = "GET";
                request.Timeout = 2500;
                request.ReadWriteTimeout = 2500;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    var parsed = JsonLite.Parse(json) as Dictionary<string, object>;
                    if (parsed != null)
                    {
                        var modelList = JsonLite.GetArray(parsed, "models");
                        if (modelList != null)
                        {
                            foreach (var item in modelList)
                            {
                                var mObj = item as Dictionary<string, object>;
                                if (mObj != null)
                                {
                                    string name = JsonLite.GetString(mObj, "name");
                                    if (string.IsNullOrEmpty(name)) name = JsonLite.GetString(mObj, "model");
                                    if (!string.IsNullOrEmpty(name) && !models.Contains(name))
                                    {
                                        models.Add(name);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // REST API không phản hồi, thử tiếp qua CLI
            }

            if (models.Count > 0) return models;

            // 2. Thử gọi CLI 'ollama list'
            try
            {
                string[] possiblePaths = new string[] {
                    "ollama",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ollama", "ollama.exe")
                };

                foreach (string exePath in possiblePaths)
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = exePath,
                            Arguments = "list",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using (Process proc = Process.Start(psi))
                        {
                            if (proc.WaitForExit(2500))
                            {
                                string output = proc.StandardOutput.ReadToEnd();
                                string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 1; i < lines.Length; i++)
                                {
                                    string line = lines[i].Trim();
                                    if (!string.IsNullOrEmpty(line))
                                    {
                                        string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                        if (parts.Length > 0 && !models.Contains(parts[0]))
                                        {
                                            models.Add(parts[0]);
                                        }
                                    }
                                }
                                if (models.Count > 0) break;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return models;
        }

        /// <summary>
        /// Quét model Ollama bất đồng bộ không làm lag/khựng giao diện UI.
        /// </summary>
        public static void ScanLocalOllamaModelsAsync(Action<List<string>, string> callback, string endpoint = null)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var list = ScanLocalOllamaModels(endpoint);
                    if (list.Count > 0)
                    {
                        callback(list, null);
                    }
                    else
                    {
                        callback(new List<string>(), "Không tìm thấy model Ollama nào đang chạy trên máy (Hãy chạy 'ollama serve' hoặc cài model).");
                    }
                }
                catch (Exception ex)
                {
                    callback(new List<string>(), ex.Message);
                }
            });
        }
        #endregion

        #region AI Error Message Customizer & Classifier
        /// <summary>
        /// Phân tích và chuyển đổi mã lỗi từ máy chủ AI thành thông báo tiếng Việt dễ hiểu, trực quan, hướng dẫn cụ thể theo yêu cầu:
        /// - Model ngừng hỗ trợ / không tồn tại (404, Not Found, deprecated, v.v.)
        /// - Model hoặc tài khoản hết Token / hết lượt / hết quota (RESOURCE_EXHAUSTED, insufficient_quota, v.v.)
        /// - Chat quá nhanh / Rate limit RPM (429 Too Many Requests, RPM, slow down, v.v.)
        /// - Sai API Key / Chưa cấp quyền (401, 403, API_KEY_INVALID)
        /// - Mất mạng / Timeout / Máy chủ gián đoạn
        /// </summary>
        public static string FormatAiErrorMessage(string rawError, int statusCode, string modelName, Exception ex = null)
        {
            string errLower = (rawError ?? "").ToLowerInvariant();
            string exLower = (ex != null && ex.Message != null) ? ex.Message.ToLowerInvariant() : "";
            string modelDisplay = !string.IsNullOrWhiteSpace(modelName) ? modelName : "hiện tại";

            // 1. Model không tồn tại hoặc đã ngừng hỗ trợ (404, Not Found, deprecated, discontinued, unknown model, v.v.)
            if (statusCode == 404 || 
                errLower.Contains("404") ||
                errLower.Contains("not_found") ||
                errLower.Contains("not found") ||
                errLower.Contains("is not supported") ||
                errLower.Contains("deprecated") ||
                errLower.Contains("discontinued") ||
                errLower.Contains("unknown model") ||
                errLower.Contains("invalid_model") ||
                errLower.Contains("does not exist") ||
                errLower.Contains("does not have access to model") ||
                exLower.Contains("404") ||
                exLower.Contains("not found"))
            {
                return string.Format("⚠️ Model '{0}' hiện không tồn tại hoặc đã ngừng hỗ trợ (404 Not Found).\n👉 Vui lòng chọn Model khác (ví dụ: gemini-3.7-flash, gemini-3.1-flash-lite, v.v.) trong Cài đặt hoặc danh sách phía trên.", modelDisplay);
            }

            // 2. Hết Token / Hết hạn ngạch sử dụng / Hết lượt / Hết tiền (RESOURCE_EXHAUSTED, insufficient_quota, credits, billing)
            if (errLower.Contains("resource_exhausted") ||
                errLower.Contains("insufficient_quota") ||
                errLower.Contains("quota exceeded") ||
                errLower.Contains("exceeded your current quota") ||
                errLower.Contains("out of tokens") ||
                errLower.Contains("token limit") ||
                errLower.Contains("billing") ||
                errLower.Contains("credit") ||
                errLower.Contains("payment required") ||
                statusCode == 402)
            {
                return string.Format("🚫 Model '{0}' hoặc tài khoản đã hết lượt / hết Token (Hạn ngạch Quota đã cạn).\n👉 Vui lòng đổi sang Model khác, đổi API Key hoặc kiểm tra lại hạn mức tài khoản.", modelDisplay);
            }

            // 3. Gửi yêu cầu quá nhanh / Đạt giới hạn RPM - Requests Per Minute (Rate Limit / Too Many Requests)
            if (statusCode == 429 ||
                errLower.Contains("rate_limit") ||
                errLower.Contains("rate limit") ||
                errLower.Contains("too many requests") ||
                errLower.Contains("requests per minute") ||
                errLower.Contains("rpm") ||
                errLower.Contains("tpm") ||
                errLower.Contains("slow down") ||
                errLower.Contains("try again in") ||
                errLower.Contains("try again after") ||
                errLower.Contains("retry after") ||
                exLower.Contains("429"))
            {
                return "⏳ Bạn đang gửi yêu cầu quá nhanh (Đạt giới hạn RPM - Requests Per Minute).\n👉 Vui lòng chat chậm lại và chờ sau ít phút trước khi gửi tiếp nhé!";
            }

            // 4. Lỗi API Key không hợp lệ / Chưa được cấp quyền (401 / 403 / API_KEY_INVALID)
            if (statusCode == 401 || statusCode == 403 ||
                errLower.Contains("401") || errLower.Contains("403") ||
                errLower.Contains("api_key_invalid") ||
                errLower.Contains("invalid_api_key") ||
                errLower.Contains("unauthorized") ||
                errLower.Contains("forbidden") ||
                errLower.Contains("permission denied") ||
                exLower.Contains("401") || exLower.Contains("403"))
            {
                return "🔑 API Key không chính xác hoặc không có quyền truy cập (401/403).\n👉 Vui lòng kiểm tra và dán lại mã API Key chính xác trong phần Cài đặt AI.";
            }

            // 5. Lỗi Timeout / Quá thời gian chờ
            if (statusCode == 408 || statusCode == 504 ||
                errLower.Contains("timeout") ||
                exLower.Contains("timeout") ||
                (ex is WebException && ((WebException)ex).Status == WebExceptionStatus.Timeout))
            {
                return "⏱️ Yêu cầu quá thời gian chờ (Server phản hồi chậm hoặc timeout).\n👉 Vui lòng thử lại sau vài giây hoặc chọn Model có tốc độ phản hồi nhanh hơn (Flash).";
            }

            // 6. Lỗi kết nối / Mất mạng / Máy chủ gián đoạn / 500 / 502 / 503 / Ollama offline
            if (statusCode == 500 || statusCode == 502 || statusCode == 503 ||
                errLower.Contains("connectfailure") ||
                errLower.Contains("nameresolutionfailure") ||
                errLower.Contains("connection refused") ||
                errLower.Contains("unable to connect") ||
                exLower.Contains("connectfailure") ||
                exLower.Contains("connection refused"))
            {
                return "🌐 Lỗi kết nối tới máy chủ AI hoặc mạng bị gián đoạn.\n👉 Vui lòng kiểm tra đường truyền Internet (hoặc đảm bảo 'ollama serve' đang chạy nếu dùng Local).";
            }

            // 7. Lỗi mặc định sạch đẹp
            if (!string.IsNullOrWhiteSpace(rawError))
            {
                return string.Format("❌ Lỗi phản hồi ({0}): {1}", statusCode > 0 ? ("HTTP " + statusCode) : "Máy chủ", rawError.Trim());
            }
            if (ex != null && !string.IsNullOrWhiteSpace(ex.Message))
            {
                return "❌ Lỗi: " + ex.Message;
            }
            return "❌ Đã xảy ra lỗi không xác định khi giao tiếp với máy chủ AI.";
        }
        #endregion
    }
}

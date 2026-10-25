using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ZenQOffice.Core
{
    public enum CanvasQuestionType
    {
        MultipleChoice, // Trắc nghiệm 4 phương án A, B, C, D
        TrueFalse,      // Đúng / Sai
        FillBlank,      // Điền khuyết
        Essay           // Tự luận
    }

    public class AiCanvasQuestion
    {
        public int Number { get; set; }
        public CanvasQuestionType Type { get; set; } = CanvasQuestionType.MultipleChoice;
        public string QuestionText { get; set; } = "";
        public List<string> Options { get; set; } = new List<string>();
        public int CorrectOptionIndex { get; set; } = 0; // 0 = A, 1 = B, 2 = C, 3 = D
        public string CorrectAnswerText { get; set; } = ""; // Dành cho FillBlank hoặc Essay
        public string Explanation { get; set; } = "";

        // Trạng thái tương tác trên Canvas
        public int SelectedOptionIndex { get; set; } = -1;
        public string UserTypedAnswer { get; set; } = "";
        public bool IsAnswered { get; set; } = false;
        public bool IsCorrect { get; set; } = false;
        public bool ShowExplanation { get; set; } = false;
    }

    public class AiCanvasQuizData
    {
        public string Title { get; set; } = "Đề ôn tập kiến thức";
        public string Description { get; set; } = "";
        public List<AiCanvasQuestion> Questions { get; set; } = new List<AiCanvasQuestion>();

        public string ToJson()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
                return JsonSerializer.Serialize(this, options);
            }
            catch
            {
                return "";
            }
        }
    }

    /// <summary>
    /// Bộ chuyển đổi và phân tích dữ liệu Canvas giữa JSON của AI, giao diện Canvas và tài liệu Writer.
    /// Tích hợp khả năng tự sửa lỗi JSON bị cắt cụt (Auto-repair truncated JSON) và đọc đa hình (Case-insensitive & Multi-type).
    /// </summary>
    public static class AiCanvasParser
    {
        /// <summary>
        /// Bóc tách cấu trúc câu hỏi Canvas từ phản hồi của AI (khối json:canvas hoặc JSON thông thường).
        /// Hỗ trợ phục hồi dữ liệu kể cả khi phản hồi vượt giới hạn token khiến JSON bị cắt ngang.
        /// </summary>
        public static bool TryParse(string rawAiText, out AiCanvasQuizData quiz)
        {
            quiz = null;
            if (string.IsNullOrWhiteSpace(rawAiText)) return false;

            try
            {
                string jsonCandidate = ExtractJsonBlock(rawAiText);
                if (string.IsNullOrWhiteSpace(jsonCandidate)) return false;

                JsonDocument doc = null;

                // 1. Thử parse trực tiếp
                try
                {
                    doc = JsonDocument.Parse(jsonCandidate);
                }
                catch
                {
                    // 2. Nếu parse thất bại (thường do AI bị cắt cụt token ở cuối), kích hoạt cơ chế tự phục hồi JSON
                    string repairedJson = RepairTruncatedJson(jsonCandidate);
                    if (!string.IsNullOrEmpty(repairedJson))
                    {
                        try
                        {
                            doc = JsonDocument.Parse(repairedJson);
                        }
                        catch
                        {
                            doc = null;
                        }
                    }
                }

                if (doc == null) return false;

                using (doc)
                {
                    var root = doc.RootElement;
                    var parsedQuiz = new AiCanvasQuizData();

                    if (TryGetProp(root, out var titleProp, "title", "Title", "name", "Name"))
                        parsedQuiz.Title = titleProp.GetString() ?? "Đề ôn tập tương tác";

                    if (TryGetProp(root, out var descProp, "description", "Description", "desc", "Desc"))
                        parsedQuiz.Description = descProp.GetString() ?? "";

                    if (TryGetProp(root, out var questionsProp, "questions", "Questions", "items", "Items") && questionsProp.ValueKind == JsonValueKind.Array)
                    {
                        int qNum = 1;
                        foreach (var qEl in questionsProp.EnumerateArray())
                        {
                            if (qEl.ValueKind != JsonValueKind.Object) continue;

                            var q = new AiCanvasQuestion();
                            q.Number = qNum++;

                            if (TryGetProp(qEl, out var numProp, "number", "Number", "id", "Id") && numProp.TryGetInt32(out int n))
                                q.Number = n;

                            // Đọc loại câu hỏi (hỗ trợ cả int enum và string)
                            if (TryGetProp(qEl, out var typeProp, "type", "Type"))
                            {
                                if (typeProp.ValueKind == JsonValueKind.Number && typeProp.TryGetInt32(out int tInt))
                                {
                                    q.Type = (CanvasQuestionType)Math.Max(0, Math.Min(3, tInt));
                                }
                                else if (typeProp.ValueKind == JsonValueKind.String)
                                {
                                    string tStr = typeProp.GetString()?.ToLowerInvariant() ?? "";
                                    if (tStr.Contains("true") || tStr.Contains("false") || tStr.Contains("dung") || tStr.Contains("sai"))
                                        q.Type = CanvasQuestionType.TrueFalse;
                                    else if (tStr.Contains("blank") || tStr.Contains("khuyet") || tStr.Contains("dien"))
                                        q.Type = CanvasQuestionType.FillBlank;
                                    else if (tStr.Contains("essay") || tStr.Contains("tuan") || tStr.Contains("tuluan"))
                                        q.Type = CanvasQuestionType.Essay;
                                    else
                                        q.Type = CanvasQuestionType.MultipleChoice;
                                }
                            }

                            // Đọc nội dung câu hỏi
                            if (TryGetProp(qEl, out var qTextProp, "question", "Question", "questionText", "QuestionText", "content", "Content"))
                                q.QuestionText = qTextProp.GetString() ?? "";

                            // Đọc danh sách phương án
                            if (TryGetProp(qEl, out var optsProp, "options", "Options", "choices", "Choices") && optsProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var optEl in optsProp.EnumerateArray())
                                {
                                    string optText = optEl.GetString();
                                    if (!string.IsNullOrEmpty(optText))
                                    {
                                        q.Options.Add(optText);
                                    }
                                }
                            }

                            // Nếu là True/False mà chưa có options, tự động thêm [Đúng, Sai]
                            if (q.Type == CanvasQuestionType.TrueFalse && q.Options.Count == 0)
                            {
                                q.Options.Add("A. Đúng");
                                q.Options.Add("B. Sai");
                            }

                            // Đọc đáp án đúng (hỗ trợ cả int và string)
                            if (TryGetProp(qEl, out var cIdxProp, "correctIndex", "CorrectIndex", "correctOptionIndex", "CorrectOptionIndex"))
                            {
                                if (cIdxProp.ValueKind == JsonValueKind.Number && cIdxProp.TryGetInt32(out int cIdx))
                                {
                                    q.CorrectOptionIndex = Math.Max(0, cIdx);
                                }
                                else if (cIdxProp.ValueKind == JsonValueKind.String)
                                {
                                    string s = cIdxProp.GetString() ?? "";
                                    if (int.TryParse(s, out int parsedIdx)) q.CorrectOptionIndex = Math.Max(0, parsedIdx);
                                    else if (s.StartsWith("A", StringComparison.OrdinalIgnoreCase)) q.CorrectOptionIndex = 0;
                                    else if (s.StartsWith("B", StringComparison.OrdinalIgnoreCase)) q.CorrectOptionIndex = 1;
                                    else if (s.StartsWith("C", StringComparison.OrdinalIgnoreCase)) q.CorrectOptionIndex = 2;
                                    else if (s.StartsWith("D", StringComparison.OrdinalIgnoreCase)) q.CorrectOptionIndex = 3;
                                }
                            }
                            else if (TryGetProp(qEl, out var cAnsProp, "correctAnswer", "CorrectAnswer", "correctAnswerText", "CorrectAnswerText"))
                            {
                                string ansStr = cAnsProp.GetString() ?? "";
                                q.CorrectAnswerText = ansStr;
                                if (ansStr.StartsWith("A", StringComparison.OrdinalIgnoreCase)) q.CorrectOptionIndex = 0;
                                else if (ansStr.StartsWith("B", StringComparison.OrdinalIgnoreCase)) q.CorrectOptionIndex = 1;
                                else if (ansStr.StartsWith("C", StringComparison.OrdinalIgnoreCase)) q.CorrectOptionIndex = 2;
                                else if (ansStr.StartsWith("D", StringComparison.OrdinalIgnoreCase)) q.CorrectOptionIndex = 3;
                            }

                            // Đọc lời giải thích
                            if (TryGetProp(qEl, out var expProp, "explanation", "Explanation", "explain", "giaiThich", "loiGiai"))
                                q.Explanation = expProp.GetString() ?? "";

                            parsedQuiz.Questions.Add(q);
                        }
                    }

                    if (parsedQuiz.Questions.Count > 0)
                    {
                        quiz = parsedQuiz;
                        return true;
                    }
                }

                quiz = null;
                return false;
            }
            catch
            {
                quiz = null;
                return false;
            }
        }

        /// <summary>
        /// Bóc tách chuỗi JSON thô từ phản hồi bất kỳ của AI
        /// </summary>
        private static string ExtractJsonBlock(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return null;

            // 1. Tìm trong thẻ ```json:canvas ... ``` hoặc ```canvas ... ``` hoặc ```json ... ```
            var mBlock = Regex.Match(rawText, @"```(?:json:canvas|canvas|json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
            if (mBlock.Success)
            {
                string inner = mBlock.Groups[1].Value.Trim();
                int b1 = inner.IndexOf('{');
                int b2 = inner.LastIndexOf('}');
                if (b1 >= 0 && b2 > b1)
                {
                    return inner.Substring(b1, b2 - b1 + 1).Trim();
                }
            }

            // 2. Nếu thẻ ``` không được đóng (bị cắt cụt giữa chừng do hết token)
            var mOpen = Regex.Match(rawText, @"```(?:json:canvas|canvas|json)?\s*(\{[\s\S]*)", RegexOptions.IgnoreCase);
            if (mOpen.Success)
            {
                return mOpen.Groups[1].Value.Trim();
            }

            // 3. Tìm trực tiếp khối JSON chứa "questions" hoặc "Questions"
            int startIdx = rawText.IndexOf("{\"questions\"", StringComparison.OrdinalIgnoreCase);
            if (startIdx < 0) startIdx = rawText.IndexOf("{\n  \"questions\"", StringComparison.OrdinalIgnoreCase);
            if (startIdx < 0) startIdx = rawText.IndexOf("{\"title\"", StringComparison.OrdinalIgnoreCase);
            if (startIdx < 0) startIdx = rawText.IndexOf('{');

            if (startIdx >= 0)
            {
                int endIdx = rawText.LastIndexOf('}');
                if (endIdx > startIdx)
                {
                    string candidate = rawText.Substring(startIdx, endIdx - startIdx + 1).Trim();
                    if (candidate.IndexOf("\"questions\"", StringComparison.OrdinalIgnoreCase) >= 0 || candidate.IndexOf("\"title\"", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return candidate;
                    }
                }
                else
                {
                    // Lấy toàn bộ từ '{' tới cuối chuỗi để cơ chế repair xử lý đóng thẻ
                    return rawText.Substring(startIdx).Trim();
                }
            }

            return null;
        }

        /// <summary>
        /// Tự động phục hồi JSON bị cắt ngang do chạm ngưỡng giới hạn token của LLM
        /// </summary>
        private static string RepairTruncatedJson(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return null;

            // Thử lùi về dấu '}' gần nhất của câu hỏi hoàn chỉnh trước đó
            int searchPos = rawJson.Length - 1;
            int attempts = 0;

            while (attempts < 25 && searchPos > 0)
            {
                int lastBrace = rawJson.LastIndexOf('}', searchPos);
                if (lastBrace <= 0) break;

                string candidate = rawJson.Substring(0, lastBrace + 1).Trim();
                candidate = candidate.TrimEnd(',').Trim();

                // Thử đóng mảng questions và object root
                string attempt1 = candidate + "\n  ]\n}";
                try
                {
                    using (var d = JsonDocument.Parse(attempt1))
                    {
                        return attempt1;
                    }
                }
                catch { }

                // Thử đóng chỉ object root nếu mảng questions đã đóng
                string attempt2 = candidate + "\n}";
                try
                {
                    using (var d = JsonDocument.Parse(attempt2))
                    {
                        return attempt2;
                    }
                }
                catch { }

                searchPos = lastBrace - 1;
                attempts++;
            }

            return null;
        }

        /// <summary>
        /// Truy vấn thuộc tính JSON không phân biệt chữ hoa, chữ thường và thử nhiều tên biến thay thế
        /// </summary>
        private static bool TryGetProp(JsonElement el, out JsonElement result, params string[] candidateNames)
        {
            result = default;
            if (el.ValueKind != JsonValueKind.Object) return false;

            foreach (var prop in el.EnumerateObject())
            {
                for (int i = 0; i < candidateNames.Length; i++)
                {
                    if (string.Equals(prop.Name, candidateNames[i], StringComparison.OrdinalIgnoreCase))
                    {
                        result = prop.Value;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Xuất định dạng chuẩn văn bản câu hỏi A, B, C, D xếp dọc xuống để nhập thẳng vào Writer.
        /// </summary>
        public static string FormatQuizForWriter(AiCanvasQuizData quiz)
        {
            if (quiz == null || quiz.Questions == null || quiz.Questions.Count == 0) return "";

            var sb = new StringBuilder();
            sb.AppendLine((quiz.Title ?? "BÀI KIỂM TRA ÔN TẬP").ToUpper());
            if (!string.IsNullOrWhiteSpace(quiz.Description))
            {
                sb.AppendLine(quiz.Description);
            }
            sb.AppendLine("Thời gian làm bài: " + Math.Max(15, quiz.Questions.Count * 2) + " phút");
            sb.AppendLine(new string('─', 54));
            sb.AppendLine();

            // 1. Xuất danh sách câu hỏi
            for (int i = 0; i < quiz.Questions.Count; i++)
            {
                var q = quiz.Questions[i];
                int qNum = q.Number > 0 ? q.Number : (i + 1);
                sb.AppendLine(string.Format("Câu {0}: {1}", qNum, q.QuestionText));

                if (q.Type == CanvasQuestionType.MultipleChoice || q.Type == CanvasQuestionType.TrueFalse)
                {
                    // Đáp án A, B, C, D xếp dọc xuống theo hàng
                    if (q.Options != null && q.Options.Count > 0)
                    {
                        for (int optIdx = 0; optIdx < q.Options.Count; optIdx++)
                        {
                            string opt = q.Options[optIdx].Trim();
                            char prefix = (char)('A' + optIdx);
                            // Chuẩn hóa tiền tố A. B. C. D.
                            if (!Regex.IsMatch(opt, @"^[A-Da-d]\."))
                            {
                                opt = string.Format("{0}. {1}", prefix, opt);
                            }
                            sb.AppendLine(opt);
                        }
                    }
                }
                else if (q.Type == CanvasQuestionType.FillBlank)
                {
                    sb.AppendLine("Đáp án: ................................................................................");
                }
                else if (q.Type == CanvasQuestionType.Essay)
                {
                    sb.AppendLine("Trả lời:");
                    sb.AppendLine("........................................................................................................................");
                    sb.AppendLine("........................................................................................................................");
                    sb.AppendLine("........................................................................................................................");
                }

                sb.AppendLine(); // Ngắt dòng giữa các câu
            }

            // 2. Bảng đáp án và hướng dẫn giải chi tiết ở cuối tài liệu
            sb.AppendLine(new string('─', 54));
            sb.AppendLine("BẢNG ĐÁP ÁN & LỜI GIẢI CHI TIẾT");
            sb.AppendLine(new string('─', 54));

            for (int i = 0; i < quiz.Questions.Count; i++)
            {
                var q = quiz.Questions[i];
                int qNum = q.Number > 0 ? q.Number : (i + 1);
                string ans = "";
                if (q.Type == CanvasQuestionType.MultipleChoice || q.Type == CanvasQuestionType.TrueFalse)
                {
                    char ansChar = (char)('A' + q.CorrectOptionIndex);
                    ans = ansChar.ToString();
                }
                else
                {
                    ans = !string.IsNullOrEmpty(q.CorrectAnswerText) ? q.CorrectAnswerText : "(Xem lời giải chi tiết)";
                }

                sb.AppendLine(string.Format("Câu {0}: {1}", qNum, ans));
                if (!string.IsNullOrWhiteSpace(q.Explanation))
                {
                    sb.AppendLine("  ↳ Lời giải: " + q.Explanation);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}

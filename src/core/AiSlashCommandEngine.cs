using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ZenQOffice.Format;
using ZenQOffice.UI;

namespace ZenQOffice.Core
{
    /// <summary>
    /// Định nghĩa cấu trúc thông tin của một lệnh gạch chéo (Slash Command)
    /// </summary>
    public class AiSlashCommand
    {
        public string Name { get; set; }           // Ví dụ: "/file"
        public string[] Aliases { get; set; }      // Ví dụ: ["/f", "/tailieu"]
        public string DisplayName { get; set; }    // Ví dụ: "/file [tệp...] [yêu cầu]"
        public string Description { get; set; }    // Mô tả ngắn gọn chức năng
        public string IconGlyph { get; set; }      // Tên glyph biểu tượng
        public bool IsFilePicker { get; set; }     // Có hỗ trợ chọn tệp từ máy không

        public bool Matches(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return false;
            string p = prefix.Trim().ToLowerInvariant();
            if (Name.ToLowerInvariant().StartsWith(p)) return true;
            if (Aliases != null)
            {
                foreach (var a in Aliases)
                {
                    if (a.ToLowerInvariant().StartsWith(p)) return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Bộ máy xử lý các lệnh Slash Commands (/, /file, /summary, /page, /translate, /table, /quiz, /explain, /help)
    /// Cho phép AI đọc đa tệp, trích xuất dữ liệu bên ngoài, tối ưu hóa token và tương tác linh hoạt.
    /// </summary>
    public static class AiSlashCommandEngine
    {
        public static readonly List<AiSlashCommand> AvailableCommands = new List<AiSlashCommand>
        {
            new AiSlashCommand
            {
                Name = "/file",
                Aliases = new[] { "/f", "/tailieu", "/tep", "/read" },
                DisplayName = "/file [tệp...] [yêu cầu]",
                Description = AppL10n.T("Đọc, đối chiếu và trích xuất dữ liệu từ các tệp bên ngoài hoặc các Tab khác", "Read, compare, and extract data from files or other open tabs"),
                IconGlyph = "/doc",
                IsFilePicker = true
            },
            new AiSlashCommand
            {
                Name = "/summary",
                Aliases = new[] { "/tomtat", "/s" },
                DisplayName = "/summary [tùy chọn]",
                Description = AppL10n.T("Tóm tắt thông minh toàn diện tài liệu theo ý chính và luận điểm cốt lõi", "Summarize key points and takeaways from the active document"),
                IconGlyph = "/list"
            },
            new AiSlashCommand
            {
                Name = "/page",
                Aliases = new[] { "/trang", "/p" },
                DisplayName = "/page [số_trang] [câu hỏi]",
                Description = AppL10n.T("Phân tích sâu, trích xuất hoặc giải đáp nội dung của một trang cụ thể (PDF/Slide)", "Analyze or explain a specific page or slide (PDF/Slides)"),
                IconGlyph = "/page"
            },
            new AiSlashCommand
            {
                Name = "/translate",
                Aliases = new[] { "/dich", "/t" },
                DisplayName = "/translate [ngôn ngữ]",
                Description = AppL10n.T("Dịch tài liệu hoặc trang hiện tại sang Tiếng Việt, Tiếng Anh, v.v.", "Translate the document or current page to another language"),
                IconGlyph = "/globe"
            },
            new AiSlashCommand
            {
                Name = "/table",
                Aliases = new[] { "/bang", "/excel" },
                DisplayName = "/table [mô tả]",
                Description = AppL10n.T("Bóc tách dữ liệu văn bản/PDF thành bảng biểu có cấu trúc (cho Calc/Excel)", "Extract text/PDF data into a structured table (for Calc/Excel)"),
                IconGlyph = "/sheet"
            },
            new AiSlashCommand
            {
                Name = "/canvas",
                Aliases = new[] { "/c", "/interactive", "/de", "/quiz", "/cauhoi", "/tracnghiem", "/q" },
                DisplayName = "/canvas [yêu cầu đề thi/ngữ liệu/bài tập...]",
                Description = AppL10n.T("Tạo không gian tương tác (trắc nghiệm, ngữ liệu đọc hiểu, đúng/sai, điền khuyết, tự luận) trên Canvas", "Generate an interactive quiz or practice workspace on Canvas"),
                IconGlyph = "/sparkle"
            },
            new AiSlashCommand
            {
                Name = "/explain",
                Aliases = new[] { "/giaithich", "/e" },
                DisplayName = "/explain [thuật ngữ / đoạn]",
                Description = AppL10n.T("Giải thích chi tiết, chuyên sâu một khái niệm, công thức hoặc đoạn văn bản", "Explain a concept, formula, or selected passage in detail"),
                IconGlyph = "/bulb"
            },
            new AiSlashCommand
            {
                Name = "/ocr",
                Aliases = new[] { "/scan" },
                DisplayName = "/ocr",
                Description = AppL10n.T("Hướng dẫn hoặc nhận diện ký tự quang học từ trang hình ảnh hoặc PDF scan", "OCR recognition tips and text extraction for scanned PDFs/images"),
                IconGlyph = "/camera"
            },
            new AiSlashCommand
            {
                Name = "/help",
                Aliases = new[] { "/?", "/trogiup" },
                DisplayName = "/help",
                Description = AppL10n.T("Xem danh sách và hướng dẫn sử dụng toàn bộ các lệnh / (Slash Commands)", "View all available / Slash Commands"),
                IconGlyph = "/help"
            }
        };

        /// <summary>
        /// Kiểm tra chuỗi nhập liệu có phải là lệnh Slash Command hay không
        /// </summary>
        public static bool IsSlashCommand(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string trimmed = text.Trim();
            return trimmed.StartsWith("/");
        }

        /// <summary>
        /// Lọc danh sách các lệnh phù hợp với từ khóa người dùng đang gõ
        /// </summary>
        public static List<AiSlashCommand> FilterCommands(string queryPrefix)
        {
            if (string.IsNullOrWhiteSpace(queryPrefix)) return new List<AiSlashCommand>(AvailableCommands);
            string q = queryPrefix.Trim().ToLowerInvariant();
            if (q == "/") return new List<AiSlashCommand>(AvailableCommands);

            return AvailableCommands.Where(c => c.Matches(q)).ToList();
        }

        /// <summary>
        /// Xử lý phân tích lệnh Slash Command trước khi gửi tới API AI.
        /// Trả về prompt bổ sung ngữ cảnh hoặc trả lời nội bộ tức thì (như /help).
        /// </summary>
        public static bool ProcessSlashCommand(
            string rawQuery,
            AppForm appForm,
            DocTab currentTab,
            out string promptToSend,
            out string instantLocalReply)
        {
            promptToSend = rawQuery;
            instantLocalReply = null;

            if (string.IsNullOrWhiteSpace(rawQuery)) return false;
            string trimmed = rawQuery.Trim();
            if (!trimmed.StartsWith("/")) return false;

            // Tách tên lệnh và tham số
            int firstSpace = trimmed.IndexOf(' ');
            string cmdName = firstSpace > 0 ? trimmed.Substring(0, firstSpace).ToLowerInvariant() : trimmed.ToLowerInvariant();
            string args = firstSpace > 0 ? trimmed.Substring(firstSpace + 1).Trim() : "";

            // 1. Lệnh /help: Phản hồi nội bộ ngay lập tức (0 token, cực nhanh)
            if (cmdName == "/help" || cmdName == "/?" || cmdName == "/trogiup")
            {
                var sbHelp = new StringBuilder();
                sbHelp.AppendLine("### 🚀 Danh sách các lệnh Slash Commands (/) của Trợ lý AI ZenQ");
                sbHelp.AppendLine("Bạn có thể gõ `/` trong ô chat để mở menu gợi ý lệnh nhanh bất cứ lúc nào:");
                sbHelp.AppendLine();
                sbHelp.AppendLine("* 📁 **`/file [tệp...] [yêu cầu]`**: Cho phép AI đọc, đối chiếu nội dung từ 1 hoặc nhiều file bên ngoài (`.docx`, `.pdf`, `.xlsx`, `.pptx`, `.txt`,...) hoặc từ các Tab khác đang mở. *Ví dụ: `/file file1.docx file2.pdf hãy so sánh điểm khác nhau`*");
                sbHelp.AppendLine("* 📋 **`/summary`**: Tóm tắt thông minh toàn diện tài liệu theo bố cục ý chính, luận điểm và kết luận.");
                sbHelp.AppendLine("* 📄 **`/page [số_trang] [câu hỏi]`**: Phân tích sâu hoặc dịch một trang cụ thể của tài liệu PDF hoặc Slide. *Ví dụ: `/page 5 tóm tắt nội dung`*");
                sbHelp.AppendLine("* 🌐 **`/translate [ngôn ngữ]`**: Dịch tài liệu hoặc trang hiện tại sang ngôn ngữ mong muốn.");
                sbHelp.AppendLine("* 📊 **`/table [mô tả]`**: Bóc tách dữ liệu văn bản/PDF thành bảng biểu có cấu trúc (sẵn sàng dán vào bảng tính Calc/Excel).");
                sbHelp.AppendLine("* ❓ **`/quiz [số câu]`**: Tự động tạo câu hỏi trắc nghiệm ôn tập kèm đáp án chi tiết dựa trên nội dung tài liệu.");
                sbHelp.AppendLine("* 💡 **`/explain [thuật ngữ]`**: Phân tích, giải thích sâu sắc về một khái niệm, công thức hoặc đoạn văn bản.");
                sbHelp.AppendLine("* 🔍 **`/ocr`**: Hướng dẫn bóc tách văn bản từ tài liệu hình ảnh hoặc PDF scan.");
                sbHelp.AppendLine();
                sbHelp.AppendLine("> **Mẹo:** Nếu bạn chỉ muốn AI làm việc trên tài liệu đang mở thì **không cần gõ `/`**, bạn cứ trò chuyện và ra lệnh tự nhiên bình thường!");
                instantLocalReply = sbHelp.ToString();
                return true;
            }

            // 2. Lệnh /file: Đọc các file bên ngoài hoặc từ các tab khác
            if (cmdName == "/file" || cmdName == "/f" || cmdName == "/tailieu" || cmdName == "/tep" || cmdName == "/read")
            {
                string remainingQuery = "";
                string externalContext = ResolveAndExtractExternalFiles(args, appForm, currentTab, out remainingQuery);

                var sbPrompt = new StringBuilder();
                if (!string.IsNullOrEmpty(externalContext))
                {
                    sbPrompt.AppendLine(externalContext);
                    sbPrompt.AppendLine();
                }
                sbPrompt.AppendLine("[CHỈ THỊ ĐẶC BIỆT TỪ LỆNH /FILE]: Người dùng đã yêu cầu xem/đối chiếu các tệp bên ngoài ở trên.");
                sbPrompt.AppendLine("Hãy phân tích kỹ lưỡng nội dung của các tệp trên và trả lời câu hỏi của người dùng dưới đây:");
                sbPrompt.AppendLine(!string.IsNullOrWhiteSpace(remainingQuery) ? remainingQuery : args);

                promptToSend = sbPrompt.ToString();
                return true;
            }

            // 3. Lệnh /summary: Tóm tắt thông minh toàn diện
            if (cmdName == "/summary" || cmdName == "/tomtat" || cmdName == "/s")
            {
                var sbPrompt = new StringBuilder();
                sbPrompt.AppendLine("[CHỈ THỊ ĐẶC BIỆT TỪ LỆNH /SUMMARY]: Hãy thực hiện tóm tắt toàn diện tài liệu này theo bố cục chuyên nghiệp:");
                sbPrompt.AppendLine("1. 🎯 **Tổng quan & Mục đích chính** của tài liệu.");
                sbPrompt.AppendLine("2. 📌 **Các luận điểm / Nội dung trọng tâm** (liệt kê rõ ràng theo từng chương/mục/phần).");
                sbPrompt.AppendLine("3. 💡 **Phân tích & Đánh giá chuyên sâu**.");
                sbPrompt.AppendLine("4. 🏁 **Kết luận & Các hành động gợi ý tiếp theo**.");
                if (!string.IsNullOrWhiteSpace(args)) sbPrompt.AppendLine(string.Format("Yêu cầu thêm từ người dùng: {0}", args));
                promptToSend = sbPrompt.ToString();
                return true;
            }

            // 4. Lệnh /page: Tập trung vào trang cụ thể
            if (cmdName == "/page" || cmdName == "/trang" || cmdName == "/p")
            {
                var mPage = Regex.Match(args, @"^(\d+)\s*(.*)$");
                int targetPageNum = 1;
                string pageQuery = args;
                if (mPage.Success)
                {
                    int.TryParse(mPage.Groups[1].Value, out targetPageNum);
                    pageQuery = mPage.Groups[2].Value;
                }

                // Tự động điều hướng trang nếu đang ở PDF hoặc Slide
                if (appForm != null)
                {
                    if (appForm.Ribbon.CurrentMode == OfficeAppMode.Pdf && appForm.PdfView != null && appForm.PdfView.Document != null)
                    {
                        int pIdx = Math.Max(0, Math.Min(targetPageNum - 1, appForm.PdfView.Document.Pages.Count - 1));
                        appForm.PdfView.ActivePageIndex = pIdx;
                        appForm.PdfView.ScrollToPage(pIdx, appForm.ClientSize.Width, appForm.ClientSize.Height);
                        appForm.Invalidate();
                    }
                    else if (appForm.Ribbon.CurrentMode == OfficeAppMode.Slides && appForm.SlidesView != null && appForm.SlidesView.Presentation != null)
                    {
                        int sIdx = Math.Max(0, Math.Min(targetPageNum - 1, appForm.SlidesView.Presentation.Slides.Count - 1));
                        appForm.SlidesView.ActiveSlideIndex = sIdx;
                        appForm.SlidesView.Invalidate();
                    }
                }

                var sbPrompt = new StringBuilder();
                sbPrompt.AppendLine(string.Format("[CHỈ THỊ ĐẶC BIỆT TỪ LỆNH /PAGE]: Người dùng đang yêu cầu tập trung làm việc trên Trang/Slide số {0}.", targetPageNum));
                sbPrompt.AppendLine(!string.IsNullOrWhiteSpace(pageQuery) ? pageQuery : string.Format("Hãy phân tích chi tiết, tóm tắt và giải thích nội dung trên Trang {0}.", targetPageNum));
                promptToSend = sbPrompt.ToString();
                return true;
            }

            // 5. Lệnh /translate: Dịch tài liệu
            if (cmdName == "/translate" || cmdName == "/dich" || cmdName == "/t")
            {
                string targetLang = !string.IsNullOrWhiteSpace(args) ? args : "Tiếng Việt";
                var sbPrompt = new StringBuilder();
                sbPrompt.AppendLine(string.Format("[CHỈ THỊ ĐẶC BIỆT TỪ LỆNH /TRANSLATE]: Hãy dịch chính xác, mượt mà nội dung sang ngôn ngữ: {0}.", targetLang));
                sbPrompt.AppendLine("Giữ nguyên thuật ngữ chuyên ngành chuẩn xác và giữ cấu trúc các đề mục.");
                promptToSend = sbPrompt.ToString();
                return true;
            }

            // 6. Lệnh /table: Bóc tách thành bảng biểu
            if (cmdName == "/table" || cmdName == "/bang" || cmdName == "/excel")
            {
                var sbPrompt = new StringBuilder();
                sbPrompt.AppendLine("[CHỈ THỊ ĐẶC BIỆT TỪ LỆNH /TABLE]: Hãy bóc tách các dữ liệu, con số hoặc danh sách trong tài liệu thành Bảng biểu có cấu trúc rõ ràng.");
                sbPrompt.AppendLine("Xuất ra bảng Markdown và kèm theo khối hành động XML để sẵn sàng chèn vào Bảng tính Calc nếu phù hợp.");
                if (!string.IsNullOrWhiteSpace(args)) sbPrompt.AppendLine("Yêu cầu cụ thể: " + args);
                promptToSend = sbPrompt.ToString();
                return true;
            }

            // 7. Lệnh /canvas (thay thế và hợp nhất cả /quiz): Tạo Canvas tương tác chuyên sâu
            if (cmdName == "/canvas" || cmdName == "/c" || cmdName == "/interactive" || cmdName == "/de" ||
                cmdName == "/quiz" || cmdName == "/cauhoi" || cmdName == "/tracnghiem" || cmdName == "/q")
            {
                var sbPrompt = new StringBuilder();
                sbPrompt.AppendLine("[CHỈ THỊ ĐẶC BIỆT TỪ LỆNH /CANVAS - INTERACTIVE WORKSPACE]:");
                sbPrompt.AppendLine("Người dùng muốn tạo không gian tương tác (Bài tập, Ngữ liệu đọc hiểu, Trắc nghiệm, Từ vựng, Bài tập thực hành) trực tiếp trên giao diện Canvas.");
                sbPrompt.AppendLine("Yêu cầu của người dùng: " + (string.IsNullOrWhiteSpace(args) ? "Tạo không gian ôn tập kiến thức 10 mục" : args));
                sbPrompt.AppendLine();
                sbPrompt.AppendLine("QUY TẮC BẮT BUỘC ĐỂ CANVAS HOẠT ĐỘNG CHUẨN XÁC:");
                sbPrompt.AppendLine("1. ĐÚNG CHỦ ĐỀ & ĐÚNG NỘI DUNG (NGHIÊM CẤM TỰ ĐỔI CHỦ ĐỀ):");
                sbPrompt.AppendLine("   - BẮT BUỘC bám sát 100% chủ đề người dùng yêu cầu (ví dụ: bộ sách, bài học Unit, tác phẩm văn học,...). Tuyệt đối KHÔNG tự ý đổi sang chủ đề khác.");
                sbPrompt.AppendLine("   - Nếu người dùng yêu cầu 'ngữ liệu' (reading passage / text material), mỗi mục trên Canvas hãy đưa đoạn trích ngữ liệu kèm các câu hỏi hoặc điểm trọng tâm tương ứng để người dùng lựa chọn, phân tích.");
                sbPrompt.AppendLine("   - Số lượng: TUÂN THỦ THEO YÊU CẦU CỦA NGƯỜI DÙNG. Tuyệt đối KHÔNG tự ý giới hạn ở 20 câu.");
                sbPrompt.AppendLine("     + Nếu người dùng yêu cầu tạo từ tài liệu đang mở ('từ file word', 'tạo từ tài liệu', 'tất cả câu',...), hãy tạo ĐẦY ĐỦ TẤT CẢ các câu hỏi có trong tài liệu.");
                sbPrompt.AppendLine("     + Nếu người dùng ghi số lượng (ví dụ '5 câu', '15 câu', '25 câu', '30 câu', '50 câu'...), hãy tạo CHÍNH XÁC số lượng đó.");
                sbPrompt.AppendLine("   - ĐÁP ÁN ĐÚNG TỪ VĂN BẢN (CỰC KỲ QUAN TRỌNG ĐỂ TRÁNH SAI ĐỀ):");
                sbPrompt.AppendLine("     + Trong ngữ liệu tài liệu đính kèm, các đáp án đúng đã được hệ thống trích xuất kèm ký hiệu [ĐÁP ÁN ĐÚNG] (do văn bản gốc có chữ màu đỏ, in đậm hoặc highlight).");
                sbPrompt.AppendLine("     + Khi sinh khối JSON Canvas, BẮT BUỘC đặt correctIndex hoặc correctAnswer đúng chuẩn theo đáp án [ĐÁP ÁN ĐÚNG] này. TUYỆT ĐỐI KHÔNG tự ý chọn đáp án khác làm sai lệch đề thi của giáo viên/người dùng!");
                sbPrompt.AppendLine("   - Dạng câu hỏi: Phân loại linh hoạt theo yêu cầu (MultipleChoice, TrueFalse, FillBlank, Essay).");
                sbPrompt.AppendLine("2. BẮT BUỘC trả về khối JSON Canvas hợp lệ nằm trong thẻ ```json:canvas ... ``` theo cấu trúc sau:");
                sbPrompt.AppendLine("```json:canvas");
                sbPrompt.AppendLine("{");
                sbPrompt.AppendLine("  \"title\": \"Tiêu đề đề thi / bài ôn tập / ngữ liệu\",");
                sbPrompt.AppendLine("  \"description\": \"Mô tả ngắn gọn\",");
                sbPrompt.AppendLine("  \"questions\": [");
                sbPrompt.AppendLine("    {");
                sbPrompt.AppendLine("      \"number\": 1,");
                sbPrompt.AppendLine("      \"type\": \"MultipleChoice\", // Hoặc \"TrueFalse\", \"FillBlank\", \"Essay\"");
                sbPrompt.AppendLine("      \"question\": \"Nội dung câu hỏi hoặc đoạn ngữ liệu cần phân tích...\",");
                sbPrompt.AppendLine("      \"options\": [\"A. ...\", \"B. ...\", \"C. ...\", \"D. ...\"], // Các phương án hoặc các ý ngữ liệu để lựa chọn");
                sbPrompt.AppendLine("      \"correctIndex\": 0, // 0 = A, 1 = B, 2 = C, 3 = D");
                sbPrompt.AppendLine("      \"correctAnswer\": \"...\", // Dành cho FillBlank hoặc gợi ý đáp án Essay");
                sbPrompt.AppendLine("      \"explanation\": \"Lời giải thích chi tiết, dịch nghĩa hoặc phân tích ngữ liệu...\"");
                sbPrompt.AppendLine("    }");
                sbPrompt.AppendLine("  ]");
                sbPrompt.AppendLine("}");
                sbPrompt.AppendLine("```");
                sbPrompt.AppendLine("3. ĐẢM BẢO HOÀN THÀNH ĐẦY ĐỦ VÀ ĐÓNG KHỐI JSON:");
                sbPrompt.AppendLine("   - Khi người dùng yêu cầu số lượng lớn (từ 15 đến 40 câu), hãy viết phần 'explanation' ngắn gọn (1-2 câu súc tích) để đảm bảo toàn bộ số câu hỏi được sinh ra đầy đủ và BẮT BUỘC đóng trọn vẹn khối ```json:canvas ... ```.");
                sbPrompt.AppendLine("   - Tuyệt đối không dừng giữa chừng khi chưa hoàn thành đủ danh sách câu hỏi.");
                sbPrompt.AppendLine("Ngoài khối json:canvas, hãy viết 1-2 câu ngắn chào mừng và tóm tắt đề vừa tạo.");
                promptToSend = sbPrompt.ToString();
                return true;
            }

            // 8. Lệnh /explain: Giải thích chuyên sâu
            if (cmdName == "/explain" || cmdName == "/giaithich" || cmdName == "/e")
            {
                var sbPrompt = new StringBuilder();
                sbPrompt.AppendLine("[CHỈ THỊ ĐẶC BIỆT TỪ LỆNH /EXPLAIN]: Hãy phân tích, giải thích sâu sắc, chi tiết và dễ hiểu về khái niệm / nội dung sau:");
                sbPrompt.AppendLine(args);
                promptToSend = sbPrompt.ToString();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tìm kiếm các tệp và trích xuất nội dung từ danh sách tham số của lệnh /file
        /// </summary>
        private static string ResolveAndExtractExternalFiles(
            string fileArgs,
            AppForm appForm,
            DocTab currentTab,
            out string remainingQuery)
        {
            remainingQuery = "";
            if (string.IsNullOrWhiteSpace(fileArgs)) return "";

            var foundFiles = new List<Tuple<string, string, string>>(); // Item1: Name, Item2: Path, Item3: Content
            var notFoundFiles = new List<string>();

            // 1. Trích xuất các chuỗi trong ngoặc kép "..."
            var quotedMatches = Regex.Matches(fileArgs, @"""([^""]+)""");
            string cleanedArgs = fileArgs;
            var candidatePaths = new List<string>();

            foreach (Match m in quotedMatches)
            {
                candidatePaths.Add(m.Groups[1].Value.Trim());
                cleanedArgs = cleanedArgs.Replace(m.Value, " ");
            }

            // 2. Trích xuất các token có đuôi mở rộng phổ biến
            var extMatches = Regex.Matches(cleanedArgs, @"(\b\S+\.(?:docx|pdf|xlsx|pptx|txt|csv|md|cs|py|json|html|xml)\b)", RegexOptions.IgnoreCase);
            foreach (Match m in extMatches)
            {
                string token = m.Groups[1].Value.Trim();
                if (!candidatePaths.Contains(token)) candidatePaths.Add(token);
                cleanedArgs = cleanedArgs.Replace(m.Value, " ");
            }

            // 3. Nếu chưa tìm thấy tệp qua đuôi hay ngoặc kép, tìm các cụm từ sau từ khóa "file" hoặc "tệp"
            if (candidatePaths.Count == 0)
            {
                var phraseMatches = Regex.Matches(fileArgs, @"(?:file|tệp|tài liệu)\s+([^\,\;\.\n]+?)(?:\s+(?:và|với|hoặc|\bvà\b|\bhãy\b|\bcho biết\b|\bso sánh\b)|$)", RegexOptions.IgnoreCase);
                foreach (Match pm in phraseMatches)
                {
                    string p = pm.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(p) && p.Length > 2 && !candidatePaths.Contains(p))
                    {
                        candidatePaths.Add(p);
                    }
                }
            }

            remainingQuery = Regex.Replace(cleanedArgs, @"\s+", " ").Trim();

            // 4. Đối chiếu từng candidatePath với các Tab đang mở hoặc các file trên ổ đĩa
            foreach (var cand in candidatePaths)
            {
                bool resolved = false;

                // A. Kiểm tra với các Tab đang mở trong ZenQ Office
                if (appForm != null && appForm.Ribbon != null && appForm.Ribbon.DocTabs != null)
                {
                    foreach (var tab in appForm.Ribbon.DocTabs)
                    {
                        if (tab == null) continue;
                        bool matchName = (!string.IsNullOrEmpty(tab.Title) && tab.Title.IndexOf(cand, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                         (!string.IsNullOrEmpty(tab.FilePath) && Path.GetFileName(tab.FilePath).IndexOf(cand, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (matchName)
                        {
                            string tabText = ExtractTextFromOpenTab(tab);
                            if (!string.IsNullOrEmpty(tabText))
                            {
                                foundFiles.Add(new Tuple<string, string, string>(tab.Title ?? "Tab", tab.FilePath ?? "[Tab Đang Mở]", tabText));
                                resolved = true;
                                break;
                            }
                        }
                    }
                }

                if (resolved) continue;

                // B. Kiểm tra đường dẫn trực tiếp trên ổ đĩa
                string fullPath = ResolveFilePathOnDisk(cand, currentTab);
                if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                {
                    string fText = ExtractTextFromArbitraryFile(fullPath, 20000);
                    foundFiles.Add(new Tuple<string, string, string>(Path.GetFileName(fullPath), fullPath, fText));
                    resolved = true;
                }
                else
                {
                    notFoundFiles.Add(cand);
                }
            }

            // 5. Đóng gói kết quả thành khối XML ngữ cảnh
            var sb = new StringBuilder();
            if (foundFiles.Count > 0)
            {
                sb.AppendLine("<external_files_context>");
                sb.AppendLine(string.Format("Số lượng tệp bên ngoài đã đọc thành công: {0}", foundFiles.Count));
                foreach (var f in foundFiles)
                {
                    sb.AppendLine(string.Format("<file name=\"{0}\" path=\"{1}\">", f.Item1, f.Item2));
                    sb.AppendLine(f.Item3);
                    sb.AppendLine("</file>");
                }
                sb.AppendLine("</external_files_context>");
            }

            if (notFoundFiles.Count > 0)
            {
                sb.AppendLine(string.Format("[LƯU Ý HỆ THỐNG]: Không tìm thấy tệp: {0}. Hệ thống đã quét tại các Tab đang mở, thư mục hiện tại, Desktop, Documents và Downloads.", string.Join(", ", notFoundFiles)));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Tìm kiếm đường dẫn tệp trên ổ đĩa (thử trực tiếp, thử thư mục tài liệu hiện tại, Desktop, Downloads, MyDocuments)
        /// </summary>
        private static string ResolveFilePathOnDisk(string pathOrName, DocTab currentTab)
        {
            if (string.IsNullOrWhiteSpace(pathOrName)) return null;

            if (File.Exists(pathOrName)) return Path.GetFullPath(pathOrName);

            List<string> searchDirs = new List<string>();
            if (currentTab != null && !string.IsNullOrEmpty(currentTab.FilePath) && File.Exists(currentTab.FilePath))
            {
                string docDir = Path.GetDirectoryName(currentTab.FilePath);
                if (!string.IsNullOrEmpty(docDir) && Directory.Exists(docDir)) searchDirs.Add(docDir);
            }

            searchDirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            searchDirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            try
            {
                string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string dlDir = Path.Combine(userProf, "Downloads");
                if (Directory.Exists(dlDir)) searchDirs.Add(dlDir);
            }
            catch { }

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(pathOrName);

            foreach (var dir in searchDirs)
            {
                if (!Directory.Exists(dir)) continue;

                // Thử ghép trực tiếp
                string direct = Path.Combine(dir, pathOrName);
                if (File.Exists(direct)) return direct;

                // Thử tìm theo mẫu *name*
                try
                {
                    var matches = Directory.GetFiles(dir, "*" + fileNameWithoutExt + "*.*", SearchOption.TopDirectoryOnly);
                    if (matches != null && matches.Length > 0)
                    {
                        return matches[0];
                    }
                }
                catch { }
            }

            return null;
        }

        /// <summary>
        /// Trích xuất nội dung từ một Tab đang mở trong ZenQ Office
        /// </summary>
        private static string ExtractTextFromOpenTab(DocTab tab)
        {
            if (tab == null) return string.Empty;

            if (tab.Mode == OfficeAppMode.Writer && tab.WriterPieceTable != null)
            {
                return SanitizeAndTrimForTokens(tab.WriterPieceTable.GetText(), 45000);
            }
            if (tab.Mode == OfficeAppMode.Pdf && tab.PdfData != null)
            {
                return SanitizeAndTrimForTokens(PdfConverterEngine.ExtractAllText(tab.PdfData, true), 20000);
            }
            if (tab.Mode == OfficeAppMode.Calc && tab.CalcSheets != null)
            {
                var sb = new StringBuilder();
                for (int sIdx = 0; sIdx < tab.CalcSheets.Count; sIdx++)
                {
                    string sName = (tab.CalcSheetNames != null && sIdx < tab.CalcSheetNames.Count) ? tab.CalcSheetNames[sIdx] : $"Sheet{sIdx+1}";
                    sb.AppendLine($"[Trang tính: {sName}]");
                    var sheet = tab.CalcSheets[sIdx];
                    int rCount = 0;
                    foreach (var kvp in sheet.GetAllCells())
                    {
                        if (rCount++ > 80) break;
                        var pos = SparseMatrix.DecodeKey(kvp.Key);
                        var cell = kvp.Value;
                        string val = !string.IsNullOrEmpty(cell.EvaluatedStr) ? cell.EvaluatedStr : cell.RawValue;
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            sb.AppendLine($"{SparseMatrix.GetCellReference(pos.Item1, pos.Item2)}: {val}");
                        }
                    }
                }
                return SanitizeAndTrimForTokens(sb.ToString(), 20000);
            }
            if (tab.Mode == OfficeAppMode.Slides && tab.PresentationData != null)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < tab.PresentationData.Slides.Count; i++)
                {
                    var s = tab.PresentationData.Slides[i];
                    sb.AppendLine($"Slide {i + 1} ({s.Title}):");
                    foreach (var sh in s.Shapes)
                    {
                        if (!string.IsNullOrWhiteSpace(sh.Text)) sb.AppendLine("  - " + sh.Text.Trim());
                    }
                }
                return SanitizeAndTrimForTokens(sb.ToString(), 20000);
            }

            return string.Empty;
        }

        /// <summary>
        /// Trích xuất nội dung văn bản từ tệp bất kỳ trên ổ đĩa (.docx, .pdf, .xlsx, .pptx, .txt, .md, .csv, code, ...)
        /// </summary>
        public static string ExtractTextFromArbitraryFile(string filePath, int maxChars = 20000)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return string.Empty;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            try
            {
                string rawText = "";

                if (ext == ".docx")
                {
                    DocxMetadata meta;
                    if (DocxHandler.LoadDocxFile(filePath, out meta) && meta != null)
                    {
                        rawText = meta.TextContent ?? "";
                    }
                }
                else if (ext == ".pdf")
                {
                    using (var doc = PdfHandler.LoadPdf(filePath))
                    {
                        if (doc != null)
                        {
                            rawText = PdfConverterEngine.ExtractAllText(doc, includePageHeaders: true);
                        }
                    }
                }
                else if (ext == ".pptx")
                {
                    using (var zip = System.IO.Compression.ZipFile.OpenRead(filePath))
                    {
                        var sb = new StringBuilder();
                        int sNum = 1;
                        foreach (var entry in zip.Entries)
                        {
                            if (entry.FullName.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase) && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                            {
                                using (var stream = entry.Open())
                                using (var reader = new StreamReader(stream, Encoding.UTF8))
                                {
                                    string xml = reader.ReadToEnd();
                                    var matches = Regex.Matches(xml, @"<a:t\b[^>]*>([\s\S]*?)</a:t>");
                                    if (matches.Count > 0)
                                    {
                                        sb.AppendLine(string.Format("--- [Slide {0}] ---", sNum));
                                        foreach (Match m in matches)
                                        {
                                            string t = System.Net.WebUtility.HtmlDecode(m.Groups[1].Value);
                                            if (!string.IsNullOrWhiteSpace(t)) sb.AppendLine(t.Trim());
                                        }
                                        sb.AppendLine();
                                    }
                                    sNum++;
                                }
                            }
                        }
                        rawText = sb.ToString();
                    }
                }
                else if (ext == ".xlsx" || ext == ".xls")
                {
                    using (var zip = System.IO.Compression.ZipFile.OpenRead(filePath))
                    {
                        var sharedStrings = new List<string>();
                        var ssEntry = zip.GetEntry("xl/sharedStrings.xml");
                        if (ssEntry != null)
                        {
                            using (var s = ssEntry.Open())
                            using (var r = new StreamReader(s, Encoding.UTF8))
                            {
                                string xml = r.ReadToEnd();
                                var matches = Regex.Matches(xml, @"<t\b[^>]*>([\s\S]*?)</t>");
                                foreach (Match m in matches)
                                {
                                    sharedStrings.Add(System.Net.WebUtility.HtmlDecode(m.Groups[1].Value));
                                }
                            }
                        }

                        var sb = new StringBuilder();
                        foreach (var entry in zip.Entries)
                        {
                            if (entry.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase) && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                            {
                                using (var s = entry.Open())
                                using (var r = new StreamReader(s, Encoding.UTF8))
                                {
                                    string xml = r.ReadToEnd();
                                    var rowMatches = Regex.Matches(xml, @"<row\b[^>]*>([\s\S]*?)</row>");
                                    int rowCount = 0;
                                    foreach (Match rm in rowMatches)
                                    {
                                        if (rowCount++ > 60) break;
                                        var cellMatches = Regex.Matches(rm.Groups[1].Value, @"<c\b([^>]*)>(?:<v>([^<]*)</v>)?");
                                        var rowVals = new List<string>();
                                        foreach (Match cm in cellMatches)
                                        {
                                            string attrs = cm.Groups[1].Value;
                                            string val = cm.Groups[2].Value;
                                            if (attrs.Contains("t=\"s\""))
                                            {
                                                int sIdx;
                                                if (int.TryParse(val, out sIdx) && sIdx >= 0 && sIdx < sharedStrings.Count)
                                                    val = sharedStrings[sIdx];
                                            }
                                            if (!string.IsNullOrWhiteSpace(val)) rowVals.Add(val);
                                        }
                                        if (rowVals.Count > 0) sb.AppendLine(string.Join(" | ", rowVals));
                                    }
                                }
                            }
                        }
                        rawText = sb.ToString();
                    }
                }
                else
                {
                    rawText = File.ReadAllText(filePath, Encoding.UTF8);
                }

                return SanitizeAndTrimForTokens(rawText, maxChars);
            }
            catch (Exception ex)
            {
                return string.Format("[Không thể đọc nội dung tệp: {0}]", ex.Message);
            }
        }

        /// <summary>
        /// Thu gọn khoảng trắng thừa và cắt gọn thông minh đầu - đuôi để tối ưu token
        /// </summary>
        public static string ExtractWriterTextWithFormatting(ZenQOffice.UI.WriterView writerView, int maxChars = 45000)
        {
            if (writerView == null || writerView.PieceTable == null) return string.Empty;
            string fullText = writerView.PieceTable.GetText();
            if (string.IsNullOrEmpty(fullText)) return string.Empty;

            var runs = writerView.FormattedRuns;
            if (runs == null || runs.Count == 0)
            {
                return SanitizeAndTrimForTokens(fullText, maxChars);
            }

            // Đánh dấu các ký tự có màu đỏ hoặc highlight/gạch chân để nhận diện đáp án đúng
            bool[] isMarkedCorrect = new bool[fullText.Length];
            for (int ri = 0; ri < runs.Count; ri++)
            {
                var run = runs[ri];
                if (run.Length <= 0 || run.StartIndex >= fullText.Length) continue;

                bool isRed = (run.Color.R >= 160 && run.Color.R > run.Color.G * 1.4f && run.Color.R > run.Color.B * 1.4f);
                bool isHl = (run.Highlight != System.Drawing.Color.Transparent && run.Highlight != System.Drawing.Color.Empty && run.Highlight.A > 0);
                bool isUnder = run.Underline;

                if (isRed || isHl || (isUnder && run.Bold))
                {
                    int end = Math.Min(fullText.Length, run.StartIndex + run.Length);
                    for (int ci = run.StartIndex; ci < end; ci++)
                    {
                        isMarkedCorrect[ci] = true;
                    }
                }
            }

            var sb = new StringBuilder();
            string[] lines = fullText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int charOffset = 0;

            for (int li = 0; li < lines.Length; li++)
            {
                string line = lines[li];
                int lineStart = charOffset;
                charOffset += line.Length + 1;

                var optionMatches = Regex.Matches(line, @"(?:^|[\s\t])([A-Da-d][\.\)])\s*");
                if (optionMatches.Count > 0)
                {
                    int lastIdx = 0;
                    for (int mi = 0; mi < optionMatches.Count; mi++)
                    {
                        var match = optionMatches[mi];
                        int optStartInLine = match.Index;
                        int optEndInLine = (mi < optionMatches.Count - 1) ? optionMatches[mi + 1].Index : line.Length;

                        bool hasCorrect = false;
                        int checkStart = Math.Min(lineStart + optStartInLine, fullText.Length - 1);
                        int checkEnd = Math.Min(lineStart + optEndInLine, fullText.Length);
                        for (int ci = checkStart; ci < checkEnd; ci++)
                        {
                            if (isMarkedCorrect[ci]) { hasCorrect = true; break; }
                        }

                        sb.Append(line.Substring(lastIdx, optEndInLine - lastIdx));
                        if (hasCorrect)
                        {
                            sb.Append(" [ĐÁP ÁN ĐÚNG]");
                        }
                        lastIdx = optEndInLine;
                    }
                    if (lastIdx < line.Length)
                    {
                        sb.Append(line.Substring(lastIdx));
                    }
                    sb.AppendLine();
                }
                else
                {
                    sb.Append(line);
                    sb.AppendLine();
                }

                if (sb.Length > maxChars) break;
            }

            return SanitizeAndTrimForTokens(sb.ToString(), maxChars);
        }

        public static string SanitizeAndTrimForTokens(string text, int maxChars = 20000)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            text = Regex.Replace(text, @"\r\n|\r", "\n");
            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @"\n{3,}", "\n\n");
            text = text.Trim();

            if (text.Length <= maxChars) return text;

            int headLen = (int)(maxChars * 0.65);
            int tailLen = maxChars - headLen - 150;
            string head = text.Substring(0, headLen);
            string tail = text.Substring(text.Length - tailLen);

            return head + "\n\n... [Đã lược bớt phần thân để tối ưu token; tài liệu còn tiếp tục] ...\n\n" + tail;
        }
    }
}

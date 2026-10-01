using System;
using System.Collections.Generic;
using System.Text;

namespace GenOffice.Core
{
    /// <summary>
    /// Bộ máy gợi ý từ ngữ thông minh (IntelliSense Word / Phrase Predictor)
    /// Độc lập với tính năng sửa lỗi chính tả.
    /// Khi người dùng gõ một từ (ví dụ "từ"), hệ thống gợi ý 3 cụm từ phổ biến tiếp theo 
    /// (như "từ đó", "từ nay", "từ biệt",...) hiển thị trong bảng popup kiểu IDE.
    /// Hỗ trợ phím Tab/Enter để chọn, mũi tên Lên/Xuống để duyệt, hoặc click chuột.
    /// </summary>
    public static class VietnameseWordPredictor
    {
        private static readonly Random _rnd = new Random();

        // Từ điển cụm từ ghép tiếng Việt phong phú theo từ gốc
        private static readonly Dictionary<string, string[]> PhraseDictionary = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "từ", new string[] { "từ đó", "từ nay", "từ biệt", "từ điển", "từ ngữ", "từ chối", "từ thiện", "từ trước", "từ lâu", "từ từ", "từ bỏ", "từ tốn" } },
            { "công", new string[] { "công việc", "công nghệ", "công ty", "công tác", "công bố", "công khai", "công nhận", "công dân", "công trình", "công cụ" } },
            { "học", new string[] { "học tập", "học sinh", "học hỏi", "học vấn", "học bổng", "học hành", "học thuật", "học kỳ", "học viên", "học thức" } },
            { "phát", new string[] { "phát triển", "phát hiện", "phát sinh", "phát biểu", "phát huy", "phát hành", "phát sóng", "phát tài", "phát quang" } },
            { "thực", new string[] { "thực hiện", "thực tế", "thực phẩm", "thực hành", "thực trạng", "thực thi", "thực lực", "thực sự", "thực chất" } },
            { "tiến", new string[] { "tiến hành", "tiến độ", "tiến bộ", "tiến lên", "tiến triển", "tiến sĩ", "tiến thoái", "tiến cử", "tiến bước" } },
            { "thông", new string[] { "thông tin", "thông báo", "thông minh", "thông qua", "thông lệ", "thông số", "thông thạo", "thông suốt", "thông điệp" } },
            { "nghiên", new string[] { "nghiên cứu", "nghiên ngẫm", "nghiên cứu sinh", "nghiên cứu khoa học" } },
            { "kinh", new string[] { "kinh tế", "kinh doanh", "kinh nghiệm", "kinh phí", "kinh điển", "kinh ngạc", "kinh thành", "kinh đô" } },
            { "quản", new string[] { "quản lý", "quản trị", "quản thúc", "quản chế", "quản giáo", "quản đốc", "quản gia" } },
            { "chính", new string[] { "chính sách", "chính phủ", "chính xác", "chính trị", "chính thức", "chính đáng", "chính kiến", "chính chuyên" } },
            { "xây", new string[] { "xây dựng", "xây cất", "xây đắp", "xây tổ", "xây nền" } },
            { "hoạt", new string[] { "hoạt động", "hoạt họa", "hoạt bát", "hoạt náo", "hoạt tính", "hoạt sắc" } },
            { "tài", new string[] { "tài liệu", "tài chính", "tài sản", "tài năng", "tài khoản", "tài trợ", "tài hoa", "tài ba" } },
            { "báo", new string[] { "báo cáo", "báo chí", "báo hiệu", "báo động", "báo giá", "báo tin", "báo cáo tài chính" } },
            { "yêu", new string[] { "yêu cầu", "yêu thương", "yêu thích", "yêu quý", "yêu dấu", "yêu kiều", "yêu mến" } },
            { "hỗ", new string[] { "hỗ trợ", "hỗn hợp", "hỗn loạn", "hỗ trợ kỹ thuật", "hỗ trợ khách hàng" } },
            { "kiểm", new string[] { "kiểm tra", "kiểm soát", "kiểm toán", "kiểm định", "kiểm duyệt", "kiểm kê", "kiểm tra chất lượng" } },
            { "hướng", new string[] { "hướng dẫn", "hướng nghiệp", "hướng tới", "hướng thiện", "hướng dẫn sử dụng", "hướng đi" } },
            { "kế", new string[] { "kế hoạch", "kế toán", "kế tiếp", "kế thừa", "kế sách", "kế hoạch kinh doanh" } },
            { "dự", new string[] { "dự án", "dự kiến", "dự báo", "dự thảo", "dự tính", "dự đoán", "dự toán", "dự định" } },
            { "hợp", new string[] { "hợp đồng", "hợp tác", "hợp lý", "hợp pháp", "hợp nhất", "hợp thời", "hợp tác xã" } },
            { "quy", new string[] { "quy định", "quy trình", "quy mô", "quy chuẩn", "quy tắc", "quy hoạch", "quy định hiện hành" } },
            { "tiêu", new string[] { "tiêu chuẩn", "tiêu chí", "tiêu biểu", "tiêu thụ", "tiêu điểm", "tiêu dùng", "tiêu diệt" } },
            { "áp", new string[] { "áp dụng", "áp đặt", "áp lực", "áp phích", "áp sát" } },
            { "kết", new string[] { "kết quả", "kết nối", "kết luận", "kết thúc", "kết hợp", "kết tinh", "kết nghĩa" } },
            { "liên", new string[] { "liên quan", "liên kết", "liên hệ", "liên hoan", "liên lạc", "liên minh", "liên hiệp" } },
            { "đảm", new string[] { "đảm bảo", "đảm nhận", "đảm đương", "đảm trách" } },
            { "hoàn", new string[] { "hoàn thành", "hoàn thiện", "hoàn toàn", "hoàn hảo", "hoàn cảnh", "hoàn trả" } },
            { "tổ", new string[] { "tổ chức", "tổ hợp", "tổ ấm", "tổ tiên", "tổ chức sự kiện" } },
            { "nhiệm", new string[] { "nhiệm vụ", "nhiệm kỳ", "nhiệm màu" } },
            { "trách", new string[] { "trách nhiệm", "trách móc", "trách cứ", "trách phận" } },
            { "văn", new string[] { "văn bản", "văn hóa", "văn phòng", "văn học", "văn minh", "văn bằng", "văn thơ" } },
            { "giải", new string[] { "giải pháp", "giải quyết", "giải thích", "giải trí", "giải thưởng", "giải trình", "giải phóng" } },
            { "đáp", new string[] { "đáp ứng", "đáp lại", "đáp từ", "đáp số", "đáp ứng nhu cầu" } },
            { "cung", new string[] { "cung cấp", "cung ứng", "cung cầu", "cung điện", "cung kính" } },
            { "thời", new string[] { "thời gian", "thời kỳ", "thời điểm", "thời tiết", "thời hạn", "thời đại", "thời khóa biểu" } },
            { "ngày", new string[] { "ngày mai", "ngày hôm nay", "ngày tháng", "ngày càng", "ngày lễ", "ngày nghỉ" } },
            { "năm", new string[] { "năm nay", "năm ngoái", "năm học", "năm tài chính", "năm tháng", "năm sau" } },
            { "tháng", new string[] { "tháng này", "tháng trước", "tháng sau", "tháng năm", "tháng lương" } },
            { "việt", new string[] { "Việt Nam", "việt hóa", "việt dã", "việt kiều" } },
            { "quốc", new string[] { "quốc gia", "quốc tế", "quốc phòng", "quốc nội", "quốc hội", "quốc kỳ", "quốc tịch" } },
            { "giáo", new string[] { "giáo dục", "giáo viên", "giáo trình", "giáo án", "giáo sư", "giáo đường" } },
            { "khoa", new string[] { "khoa học", "khoa công nghệ", "khoa phòng", "khoa bảng", "khoa mục" } },
            { "môi", new string[] { "môi trường", "môi giới", "môi sinh", "môi trường sống", "môi trường làm việc" } },
            { "an", new string[] { "an toàn", "an ninh", "an tâm", "an khang", "an toàn lao động", "an cư" } },
            { "hạnh", new string[] { "hạnh phúc", "hạnh kiểm", "hạnh ngộ", "hạnh phúc gia đình" } },
            { "thành", new string[] { "thành công", "thành phố", "thành tích", "thành lập", "thành viên", "thành quả" } },
            { "việc", new string[] { "việc làm", "việc này", "việc đó", "việc học", "việc cần làm" } },
            { "không", new string[] { "không những", "không chỉ", "không thể", "không gian", "không có", "không ngừng" } },
            { "được", new string[] { "được biết", "được coi là", "được phép", "được chọn", "được hưởng", "được công nhận" } },
            { "những", new string[] { "những điều", "những người", "những ngày", "những năm", "những vấn đề" } },
            { "trong", new string[] { "trong đó", "trong khi", "trong suốt", "trong lúc", "trong phạm vi", "trong trường hợp" } },
            { "ngoài", new string[] { "ngoài ra", "ngoài đời", "ngoài vùng", "ngoài mong đợi" } },
            { "trên", new string[] { "trên cơ sở", "trên thực tế", "trên toàn quốc", "trên thế giới" } },
            { "dưới", new string[] { "dưới đây", "dưới sự", "dưới hình thức", "dưới góc độ" } },
            { "với", new string[] { "với nhau", "với mục đích", "với tinh thần", "với vai trò" } },
            { "để", new string[] { "để có thể", "để thực hiện", "để đảm bảo", "để nâng cao" } },
            { "khi", new string[] { "khi nào", "khiến cho", "khiêm tốn", "khi cần thiết" } },
            { "nếu", new string[] { "nếu như", "nếu không", "nếu có", "nếu cần" } },
            { "như", new string[] { "như vậy", "như thế", "như sau", "như là", "như đã nêu" } },
            { "cho", new string[] { "cho biết", "cho thấy", "cho phép", "cho rằng", "cho đến khi" } },
            { "về", new string[] { "về việc", "về sau", "về cơ bản", "về phương diện" } },
            { "theo", new string[] { "theo đó", "theo quy định", "theo như", "theo đuổi", "theo yêu cầu" } },
            { "tại", new string[] { "tại sao", "tại chỗ", "tại thời điểm", "tại cơ quan" } },
            { "người", new string[] { "người dùng", "người lao động", "người quản lý", "người đại diện", "người tiêu dùng" } },
            { "hệ", new string[] { "hệ thống", "hệ sinh thái", "hệ quả", "hệ điều hành", "hệ số" } },
            { "dữ", new string[] { "dữ liệu", "dữ dội", "dữ kiện", "dữ liệu lớn" } },
            { "bảng", new string[] { "bảng tính", "bảng biểu", "bảng điểm", "bảng lương", "bảng mã" } },
            { "trang", new string[] { "trang tính", "trang chủ", "trang web", "trang thiết bị", "trang trí" } },
            { "tự", new string[] { "tự động", "tự do", "tự tin", "tự hào", "tự giác", "tự nhiên" } },
            { "bảo", new string[] { "bảo mật", "bảo vệ", "bảo hành", "bảo dưỡng", "bảo quản", "bảo hiểm" } },
            { "mở", new string[] { "mở rộng", "mở cửa", "mở đầu", "mở màn", "mở khóa" } },
            { "đóng", new string[] { "đóng góp", "đóng cửa", "đóng dấu", "đóng gói", "đóng khung" } },
            { "lưu", new string[] { "lưu trữ", "lưu ý", "lưu niệm", "lưu thông", "lưu bút" } },
            { "tải", new string[] { "tải xuống", "tải lên", "tải trọng", "tải bản ghi" } },
            { "in", new string[] { "in ấn", "in hoa", "in nghiêng", "in đậm", "in ra giấy" } },
            { "sao", new string[] { "sao chép", "sao lưu", "sao kê", "sao y" } },
            { "dán", new string[] { "dán vào", "dán nhãn", "dán định dạng" } },
            { "chọn", new string[] { "chọn lựa", "chọn lọc", "chọn tất cả" } },
            { "tìm", new string[] { "tìm kiếm", "tìm hiểu", "tìm thấy", "tìm ra" } },
            { "thay", new string[] { "thay thế", "thay đổi", "thay mặt" } }
        };

        /// <summary>
        /// Lấy 3 gợi ý phù hợp nhất hoặc ngẫu nhiên từ từ điển cụm từ
        /// </summary>
        public static List<string> GetSuggestions(string inputWord, int maxCount = 3)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrEmpty(inputWord)) return result;

            string clean = inputWord.Trim();
            if (clean.Length == 0) return result;

            string lower = clean.ToLower();

            // Xác định kiểu chữ hoa / thường
            bool isAllUpper = true;
            bool isFirstUpper = char.IsUpper(clean[0]);
            for (int i = 0; i < clean.Length; i++)
            {
                if (char.IsLetter(clean[i]) && !char.IsUpper(clean[i]))
                {
                    isAllUpper = false;
                    break;
                }
            }

            string[] candidates = null;

            // 1. Khớp chính xác từ gốc
            if (PhraseDictionary.TryGetValue(lower, out candidates))
            {
                // Có danh sách gợi ý
            }
            else
            {
                // 1b. Khớp nếu là từ Telex/VNI chưa chuyển dấu (ví dụ "twf", "tuwf" -> "từ")
                string corrected;
                if (VietnameseAutoCorrect.TryCorrectWord(lower, out corrected) && PhraseDictionary.TryGetValue(corrected, out candidates))
                {
                    // Tìm thấy theo từ đã chuẩn hóa
                }
                else
                {
                    // 1c. Khớp không dấu (ví dụ "tu" -> "từ", "cong" -> "công")
                    string unacc = RemoveDiacritics(lower);
                    List<string> unaccMatches = new List<string>();
                    foreach (var kvp in PhraseDictionary)
                    {
                        if (RemoveDiacritics(kvp.Key).Equals(unacc, StringComparison.OrdinalIgnoreCase))
                        {
                            unaccMatches.AddRange(kvp.Value);
                        }
                    }
                    if (unaccMatches.Count > 0)
                    {
                        candidates = unaccMatches.ToArray();
                    }
                }
            }

            if (candidates == null)
            {
                // 2. Tìm kiếm tiền tố nếu người dùng gõ cụm hoặc từ bắt đầu bằng prefix
                List<string> prefixMatches = new List<string>();
                foreach (var kvp in PhraseDictionary)
                {
                    if (kvp.Key.StartsWith(lower, StringComparison.OrdinalIgnoreCase))
                    {
                        prefixMatches.AddRange(kvp.Value);
                    }
                    else
                    {
                        foreach (var ph in kvp.Value)
                        {
                            if (ph.StartsWith(lower, StringComparison.OrdinalIgnoreCase))
                            {
                                prefixMatches.Add(ph);
                            }
                        }
                    }
                }
                if (prefixMatches.Count > 0)
                {
                    candidates = prefixMatches.ToArray();
                }
            }

            if (candidates != null && candidates.Length > 0)
            {
                if (candidates.Length <= maxCount)
                {
                    foreach (var c in candidates) result.Add(ApplyCasing(c, isFirstUpper, isAllUpper));
                }
                else
                {
                    // Lựa chọn 3 gợi ý ngẫu nhiên hoặc đa dạng như yêu cầu người dùng
                    List<string> pool = new List<string>(candidates);
                    for (int i = 0; i < maxCount && pool.Count > 0; i++)
                    {
                        int idx = _rnd.Next(pool.Count);
                        result.Add(ApplyCasing(pool[idx], isFirstUpper, isAllUpper));
                        pool.RemoveAt(idx);
                    }
                }
            }

            return result;
        }

        private static string ApplyCasing(string phrase, bool firstUpper, bool allUpper)
        {
            if (allUpper) return phrase.ToUpper();
            if (firstUpper)
            {
                if (phrase.Length == 1) return phrase.ToUpper();
                return char.ToUpper(phrase[0]) + phrase.Substring(1);
            }
            return phrase.ToLower();
        }

        public static string GetWordBeforePos(string text, int pos, out int wordStartPos)
        {
            wordStartPos = pos;
            if (string.IsNullOrEmpty(text) || pos <= 0 || pos > text.Length) return "";

            int start = pos - 1;
            while (start >= 0 && !VietnameseAutoCorrect.IsWordDelimiter(text[start]))
            {
                start--;
            }
            start++;
            if (start >= pos) return "";
            wordStartPos = start;
            return text.Substring(start, pos - start);
        }

        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < normalized.Length; i++)
            {
                System.Globalization.UnicodeCategory uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(normalized[i]);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(normalized[i]);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
        }
    }
}

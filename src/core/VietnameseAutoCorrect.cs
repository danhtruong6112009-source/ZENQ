using System;
using System.Collections.Generic;
using System.Text;

namespace ZenQOffice.Core
{
    /// <summary>
    /// Bộ máy tự động sửa lỗi gõ tiếng Việt (Telex & VNI Auto-Correction)
    /// Hỗ trợ sửa lỗi theo thời gian thực khi gõ phím và khi kết thúc từ (Space / Enter / Dấu câu),
    /// áp dụng cho cả 3 phân hệ: ZenQ Writer, ZenQ Slides và ZenQ Calc.
    /// Ví dụ: "twf" ➔ "từ", "tuwf" ➔ "từ", "tu72" ➔ "từ", "tooi" ➔ "tôi", "dd" ➔ "đ", "dduowjc" ➔ "được",...
    /// </summary>
    public static class VietnameseAutoCorrect
    {
        public static bool IsEnabled
        {
            get { return AppSettings.Instance.AutoCorrectVietnamese; }
        }

        // Danh sách các từ tiếng Anh ngắn phổ biến để tránh nhận nhầm thành dấu Telex
        private static readonly HashSet<string> EnglishExclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "is", "as", "us", "if", "of", "on", "in", "at", "or", "so", "to",
            "do", "no", "he", "we", "me", "be", "up", "go", "my", "by", "it",
            "an", "am", "and", "the", "for", "are", "but", "not", "you", "all",
            "any", "can", "had", "her", "was", "one", "our", "out", "day", "get",
            "has", "him", "his", "how", "man", "new", "now", "old", "see", "two",
            "way", "who", "boy", "did", "its", "let", "put", "say", "she", "too",
            "use"
        };

        // Từ điển phím tắt / viết tắt thông dụng tiếng Việt
        private static readonly Dictionary<string, string> CommonShortcuts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "dc", "được" },
            { "đc", "được" },
            { "ko", "không" },
            { "k", "không" },
            { "vs", "với" },
            { "ng", "người" },
            { "bt", "biết" },
            { "bh", "bây giờ" },
            { "sđt", "số điện thoại" },
            { "sdt", "số điện thoại" }
        };

        public static bool IsWordDelimiter(char c)
        {
            return char.IsWhiteSpace(c) || c == '.' || c == ',' || c == ';' || c == ':' ||
                   c == '!' || c == '?' || c == '(' || c == ')' || c == '[' || c == ']' ||
                   c == '{' || c == '}' || c == '"' || c == '\'' || c == '`' || c == '/' ||
                   c == '\\' || c == '-' || c == '+' || c == '=' || c == '*' || c == '<' ||
                   c == '>' || c == '~' || c == '@' || c == '#' || c == '$' || c == '%' ||
                   c == '^' || c == '&' || c == '|';
        }

        /// <summary>
        /// Xử lý phím gõ theo thời gian thực (Interactive Typing):
        /// Kiểm tra nếu phím ch vừa gõ kết hợp với từ phía trước con trỏ tạo thành ký tự hoặc từ tiếng Việt có dấu.
        /// </summary>
        public static bool TryProcessChar(string wordBeforeCursor, char ch, out int charsToRemove, out string textToInsert)
        {
            charsToRemove = 0;
            textToInsert = null;

            if (!IsEnabled || string.IsNullOrEmpty(wordBeforeCursor))
            {
                return false;
            }

            char lastChar = wordBeforeCursor[wordBeforeCursor.Length - 1];
            char lowerCh = char.ToLower(ch);
            char lowerLast = char.ToLower(lastChar);
            bool isUpper = char.IsUpper(lastChar) && (char.IsUpper(ch) || !char.IsLetter(ch));

            // 1. Phím kép phụ âm: dd -> đ, DD -> Đ
            if (lowerLast == 'd' && lowerCh == 'd')
            {
                charsToRemove = 1;
                textToInsert = char.IsUpper(lastChar) ? "Đ" : "đ";
                return true;
            }
            if (lowerLast == 'd' && ch == '9')
            {
                charsToRemove = 1;
                textToInsert = char.IsUpper(lastChar) ? "Đ" : "đ";
                return true;
            }

            // 2. Phím kép nguyên âm (Telex): aa -> â, ee -> ê, oo -> ô
            if (lowerLast == 'a' && lowerCh == 'a')
            {
                charsToRemove = 1;
                textToInsert = char.IsUpper(lastChar) ? "Â" : "â";
                return true;
            }
            if (lowerLast == 'e' && lowerCh == 'e')
            {
                charsToRemove = 1;
                textToInsert = char.IsUpper(lastChar) ? "Ê" : "ê";
                return true;
            }
            if (lowerLast == 'o' && lowerCh == 'o')
            {
                charsToRemove = 1;
                textToInsert = char.IsUpper(lastChar) ? "Ô" : "ô";
                return true;
            }

            // 3. Phím móc và trăng (Telex): w kết hợp với u, o, a
            if (lowerLast == 'o' && lowerCh == 'w')
            {
                if (wordBeforeCursor.Length >= 2 && char.ToLower(wordBeforeCursor[wordBeforeCursor.Length - 2]) == 'u')
                {
                    charsToRemove = 2;
                    textToInsert = char.IsUpper(wordBeforeCursor[wordBeforeCursor.Length - 2]) ? "ƯƠ" : "ươ";
                    return true;
                }
                charsToRemove = 1;
                textToInsert = char.IsUpper(lastChar) ? "Ơ" : "ơ";
                return true;
            }
            if (lowerLast == 'u' && lowerCh == 'w')
            {
                charsToRemove = 1;
                textToInsert = char.IsUpper(lastChar) ? "Ư" : "ư";
                return true;
            }
            if (lowerLast == 'a' && lowerCh == 'w')
            {
                charsToRemove = 1;
                textToInsert = char.IsUpper(lastChar) ? "Ă" : "ă";
                return true;
            }

            // 4. Phím VNI số: a6->â, a8->ă, e6->ê, o6->ô, o7->ơ, u7->ư
            if (lowerLast == 'a' && ch == '6') { charsToRemove = 1; textToInsert = char.IsUpper(lastChar) ? "Â" : "â"; return true; }
            if (lowerLast == 'a' && ch == '8') { charsToRemove = 1; textToInsert = char.IsUpper(lastChar) ? "Ă" : "ă"; return true; }
            if (lowerLast == 'e' && ch == '6') { charsToRemove = 1; textToInsert = char.IsUpper(lastChar) ? "Ê" : "ê"; return true; }
            if (lowerLast == 'o' && ch == '6') { charsToRemove = 1; textToInsert = char.IsUpper(lastChar) ? "Ô" : "ô"; return true; }
            if (lowerLast == 'o' && ch == '7') { charsToRemove = 1; textToInsert = char.IsUpper(lastChar) ? "Ơ" : "ơ"; return true; }
            if (lowerLast == 'u' && ch == '7') { charsToRemove = 1; textToInsert = char.IsUpper(lastChar) ? "Ư" : "ư"; return true; }

            // 5. Kiểm tra toàn bộ token: ví dụ "tw" + 'f' ➔ "từ", "tuw" + 'f' ➔ "từ", "dduowj" + 'c' ➔ "được"
            string combined = wordBeforeCursor + ch;
            string corrected;
            if (TryCorrectWord(combined, out corrected))
            {
                // Chỉ áp dụng ngay nếu:
                // a) Phím vừa gõ là phím dấu (s, f, r, x, j, 1, 2, 3, 4, 5)
                // b) Hoặc token kết thúc một mẫu hoàn chỉnh (như twf, tuwf, to6i, khoong,...)
                bool isToneKey = "sfrxj12345".IndexOf(lowerCh) >= 0;
                bool hasExplicitMarker = combined.IndexOfAny(new char[] { 'w', '7', '8', '6', '9' }) >= 0 ||
                                         combined.Contains("aa") || combined.Contains("ee") ||
                                         combined.Contains("oo") || combined.Contains("dd") ||
                                         combined.Contains("đ");

                if (isToneKey || hasExplicitMarker)
                {
                    charsToRemove = wordBeforeCursor.Length;
                    textToInsert = corrected;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sửa lỗi một từ nguyên vẹn (ví dụ khi người dùng gõ Space, Enter, hoặc kết thúc ô tính / text box).
        /// Chuyển các chuỗi Telex / VNI (twf, tuwf, dduowjc, bie6t1, to6i,...) thành tiếng Việt chuẩn.
        /// </summary>
        public static bool TryCorrectWord(string word, out string corrected)
        {
            corrected = word;
            if (!IsEnabled || string.IsNullOrEmpty(word) || word.Length < 2)
            {
                return false;
            }

            // Bỏ qua nếu là từ tiếng Anh phổ biến
            if (EnglishExclusions.Contains(word))
            {
                return false;
            }

            // Kiểm tra phím tắt thông dụng trước (dc -> được, ko -> không,...)
            string shortcutVal;
            if (CommonShortcuts.TryGetValue(word, out shortcutVal))
            {
                if (char.IsUpper(word[0]))
                {
                    if (word.Length > 1 && char.IsUpper(word[1])) corrected = shortcutVal.ToUpper();
                    else corrected = char.ToUpper(shortcutVal[0]) + (shortcutVal.Length > 1 ? shortcutVal.Substring(1) : "");
                }
                else
                {
                    corrected = shortcutVal;
                }
                return true;
            }

            string converted = ConvertTelexVniInternal(word);
            if (!string.Equals(converted, word, StringComparison.Ordinal))
            {
                corrected = converted;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sửa lỗi toàn bộ chuỗi văn bản (quét và sửa tất cả các từ telex / vni).
        /// </summary>
        public static string CorrectText(string fullText)
        {
            if (!IsEnabled || string.IsNullOrEmpty(fullText))
            {
                return fullText;
            }

            StringBuilder sb = new StringBuilder();
            StringBuilder wordBuf = new StringBuilder();

            for (int i = 0; i < fullText.Length; i++)
            {
                char c = fullText[i];
                if (IsWordDelimiter(c))
                {
                    if (wordBuf.Length > 0)
                    {
                        string w = wordBuf.ToString();
                        string corr;
                        if (TryCorrectWord(w, out corr)) sb.Append(corr);
                        else sb.Append(w);
                        wordBuf.Clear();
                    }
                    sb.Append(c);
                }
                else
                {
                    wordBuf.Append(c);
                }
            }

            if (wordBuf.Length > 0)
            {
                string w = wordBuf.ToString();
                string corr;
                if (TryCorrectWord(w, out corr)) sb.Append(corr);
                else sb.Append(w);
            }

            return sb.ToString();
        }

        private static string ConvertTelexVniInternal(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Xác định kiểu chữ hoa / thường
            bool isAllUpper = true;
            bool isFirstUpper = char.IsUpper(input[0]);
            for (int i = 0; i < input.Length; i++)
            {
                if (char.IsLetter(input[i]) && !char.IsUpper(input[i]))
                {
                    isAllUpper = false;
                    break;
                }
            }

            string s = input.ToLower();

            // 1. Chuyển VNI số sang Telex / ký tự gốc
            s = s.Replace("d9", "đ").Replace("đ9", "đ");
            s = s.Replace("a6", "â").Replace("a8", "ă");
            s = s.Replace("e6", "ê");
            s = s.Replace("o6", "ô").Replace("o7", "ơ");
            s = s.Replace("u7o7", "ươ").Replace("uo7", "ươ").Replace("u7", "ư");

            // VNI tone: 1->s, 2->f, 3->r, 4->x, 5->j, 0->remove
            s = s.Replace("1", "s").Replace("2", "f").Replace("3", "r").Replace("4", "x").Replace("5", "j").Replace("0", "");

            // 2. Telex phụ âm: dd -> đ
            s = s.Replace("dd", "đ");

            // 3. Telex nguyên âm ghép
            s = s.Replace("uow", "ươ").Replace("uwow", "ươ").Replace("uwo", "ươ");
            s = s.Replace("uơ", "ươ").Replace("ưo", "ươ");
            s = s.Replace("aa", "â").Replace("aw", "ă");
            s = s.Replace("ee", "ê");
            s = s.Replace("oo", "ô").Replace("ow", "ơ");
            s = s.Replace("uw", "ư");

            // Xử lý 'w' đứng riêng (như "twf" ➔ "tưf", "tuwf" ➔ "tưf", "wf" ➔ "ưf", "cw" ➔ "cư")
            StringBuilder sbW = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == 'w')
                {
                    if (i == 0 || IsConsonant(s[i - 1]))
                    {
                        sbW.Append('ư');
                    }
                    else if (s[i - 1] == 'u' || s[i - 1] == 'ư')
                    {
                        sbW.Append('ơ');
                    }
                    else
                    {
                        sbW.Append('ư');
                    }
                }
                else
                {
                    sbW.Append(s[i]);
                }
            }
            s = sbW.ToString();

            // 4. Trích xuất dấu thanh (s, f, r, x, j)
            char tone = '\0';
            int tonePos = -1;
            char[] toneChars = new char[] { 's', 'f', 'r', 'x', 'j' };

            for (int i = s.Length - 1; i >= 0; i--)
            {
                if (Array.IndexOf(toneChars, s[i]) >= 0)
                {
                    tone = s[i];
                    tonePos = i;
                    break;
                }
            }

            if (tone != '\0')
            {
                string baseWord = s.Substring(0, tonePos) + (tonePos + 1 < s.Length ? s.Substring(tonePos + 1) : "");
                int targetVowelIdx = FindToneTargetVowel(baseWord);
                if (targetVowelIdx >= 0)
                {
                    char targetV = baseWord[targetVowelIdx];
                    char accentedV = ApplyTone(targetV, tone);
                    if (accentedV != targetV)
                    {
                        baseWord = baseWord.Substring(0, targetVowelIdx) + accentedV + (targetVowelIdx + 1 < baseWord.Length ? baseWord.Substring(targetVowelIdx + 1) : "");
                        s = baseWord;
                    }
                }
            }

            // Khôi phục kiểu viết hoa
            if (isAllUpper && s.Length > 0)
            {
                return s.ToUpper();
            }
            else if (isFirstUpper && s.Length > 0)
            {
                return char.ToUpper(s[0]) + (s.Length > 1 ? s.Substring(1) : "");
            }
            return s;
        }

        private static bool IsConsonant(char c)
        {
            return "bcdfghjklmnpqrstvwxzđ".IndexOf(c) >= 0;
        }

        private static bool IsVowel(char c)
        {
            return "aăâeêioôơuưyáắấéếíóốớúứýàằầèềìòồờùừỳảẳẩẻểỉỏổởủửỷãẵẫẽễĩõỗỡũữỹạặậẹệịọộợụựỵ".IndexOf(c) >= 0;
        }

        private static int FindToneTargetVowel(string word)
        {
            List<int> vowelIndices = new List<int>();
            for (int i = 0; i < word.Length; i++)
            {
                if (IsVowel(word[i])) vowelIndices.Add(i);
            }

            if (vowelIndices.Count == 0) return -1;
            if (vowelIndices.Count == 1) return vowelIndices[0];

            // Ưu tiên các nguyên âm đôi/ba có trọng âm chính chuẩn tiếng Việt
            if (word.Contains("uyê")) return word.IndexOf('ê');
            if (word.Contains("iê")) return word.IndexOf('ê');
            if (word.Contains("ươ")) return word.IndexOf('ơ');
            if (word.Contains("uô")) return word.IndexOf('ô');

            bool hasEndingConsonant = vowelIndices[vowelIndices.Count - 1] < word.Length - 1;

            if (hasEndingConsonant)
            {
                // Có phụ âm cuối: dấu đặt ở nguyên âm cuối của cụm nguyên âm (như toán, hoàn, tuyết)
                return vowelIndices[vowelIndices.Count - 1];
            }
            else
            {
                // Không có phụ âm cuối:
                // ia -> i (mía, tía)
                // ua -> u (của, múa)
                // ưa -> ư (lửa, sữa)
                if (word.Contains("ia")) return word.IndexOf('i');
                if (word.Contains("ua")) return word.IndexOf('u');
                if (word.Contains("ưa")) return word.IndexOf('ư');

                // oa -> a (hòa, hóa), oe -> e (khỏe), uy -> y (thúy, thủy)
                if (word.Contains("oa")) return word.IndexOf('a');
                if (word.Contains("oe")) return word.IndexOf('e');
                if (word.Contains("uy")) return word.IndexOf('y');

                // ai, ao, au, ay, eo, êu, oi, ôi, ơi, ui, ưi, ưu -> đặt ở nguyên âm đầu
                return vowelIndices[0];
            }
        }

        private static char ApplyTone(char v, char tone)
        {
            // tone: s (sắc), f (huyền), r (hỏi), x (ngã), j (nặng)
            switch (v)
            {
                case 'a':
                    if (tone == 's') return 'á';
                    if (tone == 'f') return 'à';
                    if (tone == 'r') return 'ả';
                    if (tone == 'x') return 'ã';
                    if (tone == 'j') return 'ạ';
                    break;
                case 'ă':
                    if (tone == 's') return 'ắ';
                    if (tone == 'f') return 'ằ';
                    if (tone == 'r') return 'ẳ';
                    if (tone == 'x') return 'ẵ';
                    if (tone == 'j') return 'ặ';
                    break;
                case 'â':
                    if (tone == 's') return 'ấ';
                    if (tone == 'f') return 'ầ';
                    if (tone == 'r') return 'ẩ';
                    if (tone == 'x') return 'ẫ';
                    if (tone == 'j') return 'ậ';
                    break;
                case 'e':
                    if (tone == 's') return 'é';
                    if (tone == 'f') return 'è';
                    if (tone == 'r') return 'ẻ';
                    if (tone == 'x') return 'ẽ';
                    if (tone == 'j') return 'ẹ';
                    break;
                case 'ê':
                    if (tone == 's') return 'ế';
                    if (tone == 'f') return 'ề';
                    if (tone == 'r') return 'ể';
                    if (tone == 'x') return 'ễ';
                    if (tone == 'j') return 'ệ';
                    break;
                case 'i':
                    if (tone == 's') return 'í';
                    if (tone == 'f') return 'ì';
                    if (tone == 'r') return 'ỉ';
                    if (tone == 'x') return 'ĩ';
                    if (tone == 'j') return 'ị';
                    break;
                case 'o':
                    if (tone == 's') return 'ó';
                    if (tone == 'f') return 'ò';
                    if (tone == 'r') return 'ỏ';
                    if (tone == 'x') return 'õ';
                    if (tone == 'j') return 'ọ';
                    break;
                case 'ô':
                    if (tone == 's') return 'ố';
                    if (tone == 'f') return 'ồ';
                    if (tone == 'r') return 'ổ';
                    if (tone == 'x') return 'ỗ';
                    if (tone == 'j') return 'ộ';
                    break;
                case 'ơ':
                    if (tone == 's') return 'ớ';
                    if (tone == 'f') return 'ờ';
                    if (tone == 'r') return 'ở';
                    if (tone == 'x') return 'ỡ';
                    if (tone == 'j') return 'ợ';
                    break;
                case 'u':
                    if (tone == 's') return 'ú';
                    if (tone == 'f') return 'ù';
                    if (tone == 'r') return 'ủ';
                    if (tone == 'x') return 'ũ';
                    if (tone == 'j') return 'ụ';
                    break;
                case 'ư':
                    if (tone == 's') return 'ứ';
                    if (tone == 'f') return 'ừ';
                    if (tone == 'r') return 'ử';
                    if (tone == 'x') return 'ữ';
                    if (tone == 'j') return 'ự';
                    break;
                case 'y':
                    if (tone == 's') return 'ý';
                    if (tone == 'f') return 'ỳ';
                    if (tone == 'r') return 'ỷ';
                    if (tone == 'x') return 'ỹ';
                    if (tone == 'j') return 'ỵ';
                    break;
            }
            return v;
        }
    }
}

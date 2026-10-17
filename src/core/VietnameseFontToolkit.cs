using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using ZenQOffice.Core;

namespace ZenQOffice.Core
{
    public class ExtractedFontInfo
    {
        public string Typeface { get; set; } = "Unknown";
        public string Subfamily { get; set; } = "Regular";
        public string Format { get; set; } = "TrueType"; // TrueType, OpenType, TCVN3 (ABC), VNI-Windows
        public string FileName { get; set; } = "";
        public byte[] Data { get; set; }
        public int FileSize => Data != null ? Data.Length : 0;
        public bool IsEmbedded { get; set; }
        public bool IsInstalledOnSystem { get; set; }
        public bool HasVietnameseSupport { get; set; }
        public List<char> MissingVietnameseChars { get; set; } = new List<char>();
        public string SuggestedVietnameseFont { get; set; } = "";
        public string SourceDocument { get; set; } = "";
        public bool IsUsedInDocumentBody { get; set; } = false;
    }

    public class FontFixOptions
    {
        public bool ConvertTcvn3 { get; set; } = true;
        public bool ConvertVni { get; set; } = true;
        public bool NormalizeNfc { get; set; } = true;
        public bool ReplaceUnsupportedFonts { get; set; } = true;
        public Dictionary<string, string> CustomFontReplacements { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public class DocumentFixReport
    {
        public int TotalItemsProcessed { get; set; }
        public int TextChangesCount { get; set; }
        public int FontsReplacedCount { get; set; }
        public List<string> ReplacedFonts { get; set; } = new List<string>();
        public string OutputFilePath { get; set; } = "";
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// Bộ công cụ chuyên sâu trích xuất phông chữ nhúng từ PPTX/DOCX và sửa lỗi Tiếng Việt / vỡ font.
    /// Hỗ trợ deobfuscate .odttf (Word), bóc tách EOT .fntdata (PowerPoint), kiểm tra bảng mã cmap,
    /// chuyển mã TCVN3/VNI sang Unicode dựng sẵn và tự động ánh xạ font tiếng Việt tương đương.
    /// </summary>
    public static class VietnameseFontToolkit
    {
        // Danh sách 134 ký tự tiếng Việt có dấu chuẩn Unicode (cả hoa và thường)
        public const string AllVietnameseDiacritics =
            "áàảãạăắằẳẵặâấầẩẫậđéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵ" +
            "ÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬĐÉÈẺẼẸÊẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÚÙỦŨỤƯỨỪỬỮỰÝỲỶỸỴ";

        // Bảng đối chiếu chuyển mã TCVN3 (ABC) sang Unicode chuẩn
        private static readonly Dictionary<char, char> Tcvn3ToUnicodeMap = new Dictionary<char, char>
        {
            { '\u00B8', 'á' }, { '\u00B5', 'à' }, { '\u00B6', 'ả' }, { '\u00B7', 'ã' }, { '\u00B9', 'ạ' },
            { '\u00A8', 'ă' }, { '\u00BE', 'ắ' }, { '\u00BB', 'ằ' }, { '\u00BC', 'ẳ' }, { '\u00BD', 'ẵ' }, { '\u00C6', 'ặ' },
            { '\u00A9', 'â' }, { '\u00CA', 'ấ' }, { '\u00C7', 'ầ' }, { '\u00C8', 'ẩ' }, { '\u00C9', 'ẫ' }, { '\u00CB', 'ậ' },
            { '\u00D0', 'é' }, { '\u00CC', 'è' }, { '\u00CE', 'ẻ' }, { '\u00CF', 'ẽ' }, { '\u00D1', 'ẹ' },
            { '\u00AA', 'ê' }, { '\u00D5', 'ế' }, { '\u00D2', 'ề' }, { '\u00D3', 'ể' }, { '\u00D4', 'ễ' }, { '\u00D6', 'ệ' },
            { '\u00DD', 'í' }, { '\u00D7', 'ì' }, { '\u00D8', 'ỉ' }, { '\u00DC', 'ĩ' }, { '\u00DE', 'ị' },
            { '\u00DF', 'ó' }, { '\u00E3', 'ò' }, { '\u00E1', 'ỏ' }, { '\u00E2', 'õ' }, { '\u00E4', 'ọ' },
            { '\u00AB', 'ô' }, { '\u00E8', 'ố' }, { '\u00E5', 'ồ' }, { '\u00E6', 'ổ' }, { '\u00E7', 'ỗ' }, { '\u00E9', 'ộ' },
            { '\u00AC', 'ơ' }, { '\u00ED', 'ớ' }, { '\u00EA', 'ờ' }, { '\u00EB', 'ở' }, { '\u00EC', 'ỡ' }, { '\u00EE', 'ợ' },
            { '\u00F3', 'ú' }, { '\u00EF', 'ù' }, { '\u00F1', 'ủ' }, { '\u00F2', 'ũ' }, { '\u00F4', 'ụ' },
            { '\u00AD', 'ư' }, { '\u00F8', 'ứ' }, { '\u00F5', 'ừ' }, { '\u00F6', 'ử' }, { '\u00F7', 'ữ' }, { '\u00F9', 'ự' },
            { '\u00FD', 'ý' }, { '\u00FA', 'ỳ' }, { '\u00FB', 'ỷ' }, { '\u00FC', 'ỹ' }, { '\u00FE', 'ỵ' },
            { '\u00AE', 'đ' }, { '\u00A7', 'Đ' }
        };

        // Bảng đối chiếu VNI-Windows (2 ký tự ghép hoặc 1 ký tự) sang Unicode
        private static readonly Dictionary<string, string> VniToUnicodeMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "aù", "á" }, { "aø", "à" }, { "aû", "ả" }, { "aõ", "ã" }, { "aï", "ạ" },
            { "aé", "ắ" }, { "aè", "ằ" }, { "aú", "ẳ" }, { "aü", "ẵ" }, { "aë", "ặ" }, { "aê", "ă" },
            { "aá", "ấ" }, { "aà", "ầ" }, { "aå", "ẩ" }, { "aã", "ẫ" }, { "aä", "ậ" }, { "aâ", "â" },
            { "eù", "é" }, { "eø", "è" }, { "eû", "ẻ" }, { "eõ", "ẽ" }, { "eï", "ẹ" },
            { "eá", "ế" }, { "eà", "ề" }, { "eå", "ể" }, { "eã", "ễ" }, { "eä", "ệ" }, { "eâ", "ê" },
            { "í", "í" }, { "ì", "ì" }, { "æ", "ỉ" }, { "ó", "ó" }, { "ò", "ò" }, { "oû", "ỏ" }, { "oõ", "õ" }, { "oï", "ọ" },
            { "oá", "ố" }, { "oà", "ồ" }, { "oå", "ổ" }, { "oã", "ỗ" }, { "oä", "ộ" }, { "oâ", "ô" },
            { "ôù", "ớ" }, { "ôø", "ờ" }, { "ôû", "ở" }, { "ôõ", "ỡ" }, { "ôï", "ợ" }, { "ô", "ơ" },
            { "uù", "ú" }, { "uø", "ù" }, { "uû", "ủ" }, { "uõ", "ũ" }, { "uï", "ụ" },
            { "öù", "ứ" }, { "öø", "ừ" }, { "öû", "ử" }, { "öõ", "ữ" }, { "öï", "ự" }, { "ö", "ư" },
            { "yù", "ý" }, { "yø", "ỳ" }, { "yû", "ỷ" }, { "yõ", "ỹ" }, { "î", "ỵ" },
            { "ñ", "đ" }, { "Ñ", "Đ" },
            { "AÙ", "Á" }, { "AØ", "À" }, { "AÛ", "Ả" }, { "AÕ", "Ã" }, { "AÏ", "Ạ" },
            { "AÉ", "Ắ" }, { "AÈ", "Ằ" }, { "AÚ", "Ẳ" }, { "AÜ", "Ẵ" }, { "AË", "Ặ" }, { "AÊ", "Ă" },
            { "AÁ", "Ấ" }, { "AÀ", "Ầ" }, { "AÅ", "Ẩ" }, { "AÃ", "Ẫ" }, { "AÄ", "Ậ" }, { "AÂ", "Â" },
            { "EÙ", "É" }, { "EØ", "È" }, { "EÛ", "Ẻ" }, { "EÕ", "Ẽ" }, { "EÏ", "Ẹ" },
            { "EÁ", "Ế" }, { "EÀ", "Ề" }, { "EÅ", "Ể" }, { "EÃ", "Ễ" }, { "EÄ", "Ệ" }, { "EÂ", "Ê" },
            { "OÙ", "Ó" }, { "OØ", "Ò" }, { "OÛ", "Ỏ" }, { "OÕ", "Õ" }, { "OÏ", "Ọ" },
            { "OÁ", "Ố" }, { "OÀ", "Ồ" }, { "OÅ", "Ổ" }, { "OÃ", "Ỗ" }, { "OÄ", "Ộ" }, { "OÂ", "Ô" },
            { "ÔÙ", "Ớ" }, { "ÔØ", "Ờ" }, { "ÔÛ", "Ở" }, { "ÔÕ", "Ỡ" }, { "ÔÏ", "Ợ" },
            { "UÙ", "Ú" }, { "UØ", "Ù" }, { "UÛ", "Ủ" }, { "UÕ", "Ũ" }, { "UÏ", "Ụ" },
            { "ÖÙ", "Ứ" }, { "ÖØ", "Ừ" }, { "ÖÛ", "Ử" }, { "ÖÕ", "Ữ" }, { "ÖÏ", "Ự" }, { "Ö", "Ư" },
            { "YÙ", "Ý" }, { "YØ", "Ỳ" }, { "YÛ", "Ỷ" }, { "YÕ", "Ỹ" }, { "Î", "Ỵ" }
        };

        /// <summary>
        /// Deobfuscate phông chữ .odttf từ Word (ECMA-376 Part 2 XOR algorithm)
        /// </summary>
        public static byte[] DeobfuscateOdttf(byte[] data, string guidString)
        {
            if (data == null || data.Length == 0) return null;
            byte[] result = (byte[])data.Clone();

            try
            {
                if (string.IsNullOrEmpty(guidString))
                {
                    // Thử tìm GUID từ filename hoặc string
                    return result;
                }

                string cleanGuid = guidString.Trim('{', '}').Replace("-", "");
                if (cleanGuid.Length != 32) return result;

                Guid guid = new Guid(guidString);
                byte[] guidBytes = guid.ToByteArray();

                int len = Math.Min(32, result.Length);
                for (int i = 0; i < len; i++)
                {
                    int gi = guidBytes.Length - (i % guidBytes.Length) - 1;
                    result[i] ^= guidBytes[gi];
                }
            }
            catch
            {
                // Fallback nếu GUID parse lỗi
            }

            return result;
        }

        /// <summary>
        /// Bóc tách TrueType / OpenType font từ luồng EOT .fntdata (PowerPoint)
        /// </summary>
        public static byte[] ExtractEotFont(byte[] fntdata, out bool isOtf)
        {
            isOtf = false;
            if (fntdata == null || fntdata.Length < 36) return null;

            try
            {
                // Kiểm tra trực tiếp xem có phải TTF/OTF nguyên bản không
                if (fntdata.Length >= 4)
                {
                    uint sig = ((uint)fntdata[0] << 24) | ((uint)fntdata[1] << 16) | ((uint)fntdata[2] << 8) | fntdata[3];
                    if (sig == 0x00010000 || sig == 0x74727565) return fntdata;
                    if (sig == 0x4F54544F) { isOtf = true; return fntdata; }
                }

                uint eotSize = BitConverter.ToUInt32(fntdata, 0);
                uint fontSize = BitConverter.ToUInt32(fntdata, 4);

                if (eotSize > fontSize && fontSize > 0 && eotSize <= (uint)fntdata.Length)
                {
                    int offset = (int)(eotSize - fontSize);
                    if (offset >= 0 && offset + fontSize <= fntdata.Length)
                    {
                        byte[] fontBytes = new byte[fontSize];
                        Array.Copy(fntdata, offset, fontBytes, 0, fontSize);

                        if (fontSize >= 4 && fontBytes[0] == 0x4F && fontBytes[1] == 0x54 && fontBytes[2] == 0x54 && fontBytes[3] == 0x4F)
                        {
                            isOtf = true;
                        }
                        return fontBytes;
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Phân tích bảng cmap để xác định font có hỗ trợ đầy đủ 134 ký tự tiếng Việt hay không
        /// </summary>
        public static bool CheckFontVietnameseSupport(byte[] fontData, out List<char> missingChars)
        {
            missingChars = new List<char>();
            if (fontData == null || fontData.Length < 12) return false;

            try
            {
                ushort numTables = (ushort)((fontData[4] << 8) | fontData[5]);
                int cmapOffset = -1;

                for (int i = 0; i < numTables; i++)
                {
                    int recordOffset = 12 + i * 16;
                    if (recordOffset + 16 > fontData.Length) break;

                    string tag = Encoding.ASCII.GetString(fontData, recordOffset, 4);
                    if (tag == "cmap")
                    {
                        cmapOffset = (int)(((uint)fontData[recordOffset + 8] << 24) |
                                           ((uint)fontData[recordOffset + 9] << 16) |
                                           ((uint)fontData[recordOffset + 10] << 8) |
                                           fontData[recordOffset + 11]);
                        break;
                    }
                }

                if (cmapOffset < 0 || cmapOffset + 4 > fontData.Length) return false;

                ushort numSubtables = (ushort)((fontData[cmapOffset + 2] << 8) | fontData[cmapOffset + 3]);
                int targetSubtableOffset = -1;

                for (int j = 0; j < numSubtables; j++)
                {
                    int rec = cmapOffset + 4 + j * 8;
                    if (rec + 8 > fontData.Length) break;

                    ushort platformId = (ushort)((fontData[rec] << 8) | fontData[rec + 1]);
                    ushort encodingId = (ushort)((fontData[rec + 2] << 8) | fontData[rec + 3]);
                    uint subOffset = ((uint)fontData[rec + 4] << 24) |
                                     ((uint)fontData[rec + 5] << 16) |
                                     ((uint)fontData[rec + 6] << 8) |
                                     fontData[rec + 7];

                    // Ưu tiên Windows Unicode BMP (Platform 3, Encoding 1) hoặc Unicode (Platform 0)
                    if ((platformId == 3 && encodingId == 1) || platformId == 0)
                    {
                        targetSubtableOffset = cmapOffset + (int)subOffset;
                        break;
                    }
                    if (platformId == 3 && encodingId == 10) // Full UTF-32
                    {
                        targetSubtableOffset = cmapOffset + (int)subOffset;
                    }
                }

                if (targetSubtableOffset < 0 || targetSubtableOffset + 6 > fontData.Length) return false;

                ushort format = (ushort)((fontData[targetSubtableOffset] << 8) | fontData[targetSubtableOffset + 1]);
                HashSet<int> supportedCodePoints = new HashSet<int>();

                if (format == 4) // Format 4: Segment mapping to delta values
                {
                    ushort segCountX2 = (ushort)((fontData[targetSubtableOffset + 6] << 8) | fontData[targetSubtableOffset + 7]);
                    int segCount = segCountX2 / 2;

                    int endCodeBase = targetSubtableOffset + 14;
                    int startCodeBase = endCodeBase + segCount * 2 + 2;

                    if (startCodeBase + segCount * 2 <= fontData.Length)
                    {
                        for (int s = 0; s < segCount; s++)
                        {
                            ushort endCode = (ushort)((fontData[endCodeBase + s * 2] << 8) | fontData[endCodeBase + s * 2 + 1]);
                            ushort startCode = (ushort)((fontData[startCodeBase + s * 2] << 8) | fontData[startCodeBase + s * 2 + 1]);

                            for (int c = startCode; c <= endCode && c < 0xFFFF; c++)
                            {
                                supportedCodePoints.Add(c);
                            }
                        }
                    }
                }
                else if (format == 12) // Format 12: Segmented coverage
                {
                    if (targetSubtableOffset + 16 <= fontData.Length)
                    {
                        uint nGroups = ((uint)fontData[targetSubtableOffset + 12] << 24) |
                                       ((uint)fontData[targetSubtableOffset + 13] << 16) |
                                       ((uint)fontData[targetSubtableOffset + 14] << 8) |
                                       fontData[targetSubtableOffset + 15];

                        int groupBase = targetSubtableOffset + 16;
                        for (uint g = 0; g < nGroups && groupBase + g * 12 + 12 <= fontData.Length; g++)
                        {
                            int gOff = groupBase + (int)g * 12;
                            uint startChar = ((uint)fontData[gOff] << 24) | ((uint)fontData[gOff + 1] << 16) | ((uint)fontData[gOff + 2] << 8) | fontData[gOff + 3];
                            uint endChar = ((uint)fontData[gOff + 4] << 24) | ((uint)fontData[gOff + 5] << 16) | ((uint)fontData[gOff + 6] << 8) | fontData[gOff + 7];

                            for (uint c = startChar; c <= endChar && c <= 0x10FFFF; c++)
                            {
                                supportedCodePoints.Add((int)c);
                            }
                        }
                    }
                }

                // Kiểm tra 134 ký tự tiếng Việt
                foreach (char c in AllVietnameseDiacritics)
                {
                    if (!supportedCodePoints.Contains((int)c))
                    {
                        missingChars.Add(c);
                    }
                }

                return missingChars.Count == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Trích xuất tên Family Name từ bảng 'name' trong tệp TTF/OTF
        /// </summary>
        public static string ReadFontFamilyName(byte[] fontData, out string subfamily)
        {
            subfamily = "Regular";
            if (fontData == null || fontData.Length < 12) return "CustomFont";

            try
            {
                ushort numTables = (ushort)((fontData[4] << 8) | fontData[5]);
                int nameOffset = -1;

                for (int i = 0; i < numTables; i++)
                {
                    int rec = 12 + i * 16;
                    if (rec + 16 > fontData.Length) break;
                    string tag = Encoding.ASCII.GetString(fontData, rec, 4);
                    if (tag == "name")
                    {
                        nameOffset = (int)(((uint)fontData[rec + 8] << 24) |
                                           ((uint)fontData[rec + 9] << 16) |
                                           ((uint)fontData[rec + 10] << 8) |
                                           fontData[rec + 11]);
                        break;
                    }
                }

                if (nameOffset < 0 || nameOffset + 6 > fontData.Length) return "CustomFont";

                ushort count = (ushort)((fontData[nameOffset + 2] << 8) | fontData[nameOffset + 3]);
                ushort stringOffset = (ushort)((fontData[nameOffset + 4] << 8) | fontData[nameOffset + 5]);
                int strTable = nameOffset + stringOffset;

                string familyName = null;

                for (int j = 0; j < count; j++)
                {
                    int rec = nameOffset + 6 + j * 12;
                    if (rec + 12 > fontData.Length) break;

                    ushort platformId = (ushort)((fontData[rec] << 8) | fontData[rec + 1]);
                    ushort nameId = (ushort)((fontData[rec + 6] << 8) | fontData[rec + 7]);
                    ushort len = (ushort)((fontData[rec + 8] << 8) | fontData[rec + 9]);
                    ushort offset = (ushort)((fontData[rec + 10] << 8) | fontData[rec + 11]);

                    if (strTable + offset + len <= fontData.Length)
                    {
                        string strVal = null;
                        if (platformId == 0 || platformId == 3) // Unicode or Windows: UTF-16BE
                        {
                            strVal = Encoding.BigEndianUnicode.GetString(fontData, strTable + offset, len);
                        }
                        else if (platformId == 1) // Mac: Latin1
                        {
                            strVal = Encoding.Latin1.GetString(fontData, strTable + offset, len);
                        }

                        if (!string.IsNullOrEmpty(strVal))
                        {
                            if (nameId == 1 && string.IsNullOrEmpty(familyName)) familyName = strVal;
                            if (nameId == 2 && subfamily == "Regular") subfamily = strVal;
                        }
                    }
                }

                return !string.IsNullOrEmpty(familyName) ? familyName : "CustomFont";
            }
            catch
            {
                return "CustomFont";
            }
        }

        /// <summary>
        /// Gợi ý phông chữ tiếng Việt tương đương có hỗ trợ 100% tiếng Việt
        /// </summary>
        public static string GetSuggestedVietnameseFont(string fontName)
        {
            if (string.IsNullOrEmpty(fontName)) return "Segoe UI";
            string fn = fontName.ToLowerInvariant();

            // 1. Phông Serif / Cổ điển / Báo chí
            if (fn.Contains("serif") || fn.Contains("roman") || fn.Contains("amstrong") || fn.Contains("gothic") ||
                fn.Contains("bodoni") || fn.Contains("georgia") || fn.Contains("garamond") || fn.Contains("baskerville") ||
                fn.Contains("playfair") || fn.Contains("didot"))
            {
                return "Noto Serif";
            }

            // 2. Phông Dày / Tiêu đề / Condensed / Heavy
            if (fn.Contains("anton") || fn.Contains("impact") || fn.Contains("bebas") || fn.Contains("heavy") ||
                fn.Contains("black") || fn.Contains("condensed") || fn.Contains("oswald") || fn.Contains("knockout"))
            {
                return "Oswald";
            }

            // 3. Phông Chữ viết tay / Script / Cursive
            if (fn.Contains("script") || fn.Contains("hand") || fn.Contains("brush") || fn.Contains("calligraphy") ||
                fn.Contains("pacifico") || fn.Contains("dancing") || fn.Contains("great vibes") || fn.Contains("lobster"))
            {
                return "Dancing Script";
            }

            // 4. Phông Sans-serif Hiện đại / Hình học
            if (fn.Contains("muli") || fn.Contains("proxima") || fn.Contains("montserrat") || fn.Contains("helvetica") ||
                fn.Contains("futura") || fn.Contains("gotham") || fn.Contains("poppins") || fn.Contains("raleway") ||
                fn.Contains("avenir") || fn.Contains("circular"))
            {
                return "Montserrat";
            }

            // Mặc định cho hiển thị rõ ràng, đẹp mắt
            return "Be Vietnam Pro";
        }

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern uint GetFontUnicodeRanges(IntPtr hdc, IntPtr lpgs);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private static readonly HashSet<string> _systemFontNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool _systemFontsLoaded = false;
        private static readonly object _fontLock = new object();

        // Danh sách phông chữ Unicode thông dụng hỗ trợ đầy đủ 100% tiếng Việt
        private static readonly HashSet<string> FullVietnameseUnicodeFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Times New Roman", "Arial", "Calibri", "Segoe UI", "Tahoma", "Georgia", 
            "Cambria", "Verdana", "Trebuchet MS", "Roboto", "Open Sans", "Be Vietnam Pro", 
            "Montserrat", "Aptos", "Noto Serif", "Noto Sans", "Mulish", "Inter", 
            "Lora", "Merriweather", "Helvetica", "Consolas", "Courier New", "Palatino Linotype",
            "Segoe UI Variable", "Aptos Display", "Lucida Sans Unicode", "Arial Unicode MS"
        };

        public static bool IsSystemFontInstalled(string fontName)
        {
            if (string.IsNullOrEmpty(fontName)) return false;
            lock (_fontLock)
            {
                if (!_systemFontsLoaded)
                {
                    try
                    {
                        using (var ifc = new System.Drawing.Text.InstalledFontCollection())
                        {
                            foreach (var f in ifc.Families)
                            {
                                _systemFontNames.Add(f.Name);
                            }
                        }
                    }
                    catch { }
                    _systemFontsLoaded = true;
                }
                return _systemFontNames.Contains(fontName) || SlideFontManager.HasFont(fontName);
            }
        }

        public static bool CheckInstalledFontVietnameseSupport(string fontName, out List<char> missingChars)
        {
            missingChars = new List<char>();
            if (string.IsNullOrEmpty(fontName)) return false;

            if (FullVietnameseUnicodeFonts.Contains(fontName))
            {
                return true;
            }

            if (fontName.StartsWith(".Vn", StringComparison.OrdinalIgnoreCase) || fontName.StartsWith(".vn", StringComparison.OrdinalIgnoreCase) ||
                fontName.StartsWith("VNI-", StringComparison.OrdinalIgnoreCase) || fontName.StartsWith("vni-", StringComparison.OrdinalIgnoreCase))
            {
                foreach (char c in "áàảãạăắằẳẵặâấầẩẫậđéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵ")
                {
                    missingChars.Add(c);
                }
                return false;
            }

            try
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    try
                    {
                        using (Font font = SlideFontManager.CreateFont(fontName, 12f, FontStyle.Regular, GraphicsUnit.Point))
                        {
                            IntPtr hFont = font.ToHfont();
                            IntPtr oldFont = SelectObject(hdc, hFont);

                            try
                            {
                                uint size = GetFontUnicodeRanges(hdc, IntPtr.Zero);
                                if (size > 16)
                                {
                                    IntPtr buffer = Marshal.AllocHGlobal((int)size);
                                    try
                                    {
                                        GetFontUnicodeRanges(hdc, buffer);
                                        int cRanges = Marshal.ReadInt32(buffer, 12);
                                        List<Tuple<int, int>> ranges = new List<Tuple<int, int>>(cRanges);
                                        int offset = 16;
                                        for (int i = 0; i < cRanges; i++)
                                        {
                                            ushort low = (ushort)Marshal.ReadInt16(buffer, offset);
                                            ushort count = (ushort)Marshal.ReadInt16(buffer, offset + 2);
                                            offset += 4;
                                            ranges.Add(Tuple.Create((int)low, (int)(low + count - 1)));
                                        }

                                        foreach (char c in AllVietnameseDiacritics)
                                        {
                                            int code = (int)c;
                                            bool found = false;
                                            for (int r = 0; r < ranges.Count; r++)
                                            {
                                                if (code >= ranges[r].Item1 && code <= ranges[r].Item2)
                                                {
                                                    found = true;
                                                    break;
                                                }
                                            }
                                            if (!found) missingChars.Add(c);
                                        }

                                        return missingChars.Count == 0;
                                    }
                                    finally
                                    {
                                        Marshal.FreeHGlobal(buffer);
                                    }
                                }
                            }
                            finally
                            {
                                SelectObject(hdc, oldFont);
                                DeleteObject(hFont);
                            }
                        }
                    }
                    finally
                    {
                        ReleaseDC(IntPtr.Zero, hdc);
                    }
                }
            }
            catch { }

            return missingChars.Count == 0;
        }

        /// <summary>
        /// Trích xuất một đoạn văn bản thực tế từ tài liệu để làm nội dung xem trước
        /// </summary>
        public static string ExtractDocumentSampleText(string filePath, int maxLength = 120)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            try
            {
                if (ext == ".docx")
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
                    {
                        var entry = zip.GetEntry("word/document.xml");
                        if (entry != null)
                        {
                            XmlDocument doc = new XmlDocument();
                            using (Stream s = entry.Open()) doc.Load(s);
                            XmlNodeList tNodes = doc.SelectNodes("//*[local-name()='t']");
                            if (tNodes != null)
                            {
                                StringBuilder sb = new StringBuilder();
                                foreach (XmlNode t in tNodes)
                                {
                                    string text = t.InnerText?.Trim();
                                    if (!string.IsNullOrEmpty(text) && text.Length > 2)
                                    {
                                        if (sb.Length > 0 && !sb.ToString().EndsWith(" ") && !sb.ToString().EndsWith("-"))
                                        {
                                            sb.Append(" - ");
                                        }
                                        sb.Append(text);
                                        if (sb.Length >= maxLength) break;
                                    }
                                }
                                if (sb.Length > 0)
                                {
                                    string res = sb.ToString();
                                    if (res.Length > maxLength) res = res.Substring(0, maxLength).TrimEnd() + "...";
                                    return res;
                                }
                            }
                        }
                    }
                }
                else if (ext == ".pptx")
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
                    {
                        foreach (var entry in zip.Entries)
                        {
                            if (entry.FullName.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase) && entry.FullName.EndsWith(".xml"))
                            {
                                XmlDocument doc = new XmlDocument();
                                using (Stream s = entry.Open()) doc.Load(s);
                                XmlNodeList tNodes = doc.SelectNodes("//*[local-name()='t']");
                                if (tNodes != null)
                                {
                                    StringBuilder sb = new StringBuilder();
                                    foreach (XmlNode t in tNodes)
                                    {
                                        string text = t.InnerText?.Trim();
                                        if (!string.IsNullOrEmpty(text) && text.Length > 2)
                                        {
                                            if (sb.Length > 0 && !sb.ToString().EndsWith(" ") && !sb.ToString().EndsWith("-"))
                                            {
                                                sb.Append(" - ");
                                            }
                                            sb.Append(text);
                                            if (sb.Length >= maxLength) break;
                                        }
                                    }
                                    if (sb.Length > 0)
                                    {
                                        string res = sb.ToString();
                                        if (res.Length > maxLength) res = res.Substring(0, maxLength).TrimEnd() + "...";
                                        return res;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Quét toàn bộ phông chữ trong tệp PPTX, DOCX hoặc TTF/OTF
        /// </summary>
        public static List<ExtractedFontInfo> AnalyzeDocumentFonts(string filePath)
        {
            List<ExtractedFontInfo> results = new List<ExtractedFontInfo>();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return results;

            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            try
            {
                if (ext == ".pptx")
                {
                    results.AddRange(AnalyzePptxFonts(filePath));
                }
                else if (ext == ".docx")
                {
                    results.AddRange(AnalyzeDocxFonts(filePath));
                }
                else if (ext == ".ttf" || ext == ".otf")
                {
                    byte[] d = File.ReadAllBytes(filePath);
                    string subfam;
                    string fam = ReadFontFamilyName(d, out subfam);
                    List<char> missing;
                    bool viSupport = CheckFontVietnameseSupport(d, out missing);

                    results.Add(new ExtractedFontInfo
                    {
                        Typeface = fam,
                        Subfamily = subfam,
                        Format = ext == ".otf" ? "OpenType" : "TrueType",
                        FileName = Path.GetFileName(filePath),
                        Data = d,
                        IsEmbedded = false,
                        IsInstalledOnSystem = IsSystemFontInstalled(fam),
                        HasVietnameseSupport = viSupport,
                        MissingVietnameseChars = missing,
                        SuggestedVietnameseFont = viSupport ? fam : GetSuggestedVietnameseFont(fam),
                        SourceDocument = filePath,
                        IsUsedInDocumentBody = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("AnalyzeDocumentFonts error: " + ex.Message);
            }

            return results;
        }

        private static List<ExtractedFontInfo> AnalyzePptxFonts(string pptxPath)
        {
            List<ExtractedFontInfo> list = new List<ExtractedFontInfo>();
            Dictionary<string, ExtractedFontInfo> map = new Dictionary<string, ExtractedFontInfo>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> bodyFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (FileStream fs = new FileStream(pptxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                // 1. Quét phông thực tế trong toàn bộ slide XML
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string fn = entry.FullName.Replace('\\', '/');
                    if (fn.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase) && fn.EndsWith(".xml"))
                    {
                        try
                        {
                            XmlDocument sDoc = new XmlDocument();
                            using (Stream s = entry.Open()) sDoc.Load(s);
                            XmlNodeList fontNodes = sDoc.SelectNodes("//*[local-name()='latin' or local-name()='ea' or local-name()='cs']");
                            if (fontNodes != null)
                            {
                                foreach (XmlNode fNode in fontNodes)
                                {
                                    if (fNode.Attributes["typeface"] != null)
                                    {
                                        string tf = fNode.Attributes["typeface"].Value?.Trim();
                                        if (!string.IsNullOrEmpty(tf) && !tf.StartsWith("+") && !tf.StartsWith("minor") && !tf.StartsWith("major"))
                                        {
                                            bodyFonts.Add(tf);
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                // 2. Quét theme và presentation
                ZipArchiveEntry themeEntry = archive.GetEntry("ppt/theme/theme1.xml");
                if (themeEntry != null)
                {
                    try
                    {
                        XmlDocument tDoc = new XmlDocument();
                        using (Stream s = themeEntry.Open()) tDoc.Load(s);
                        XmlNodeList tFonts = tDoc.SelectNodes("//*[local-name()='latin']");
                        if (tFonts != null)
                        {
                            foreach (XmlNode tfNode in tFonts)
                            {
                                if (tfNode.Attributes["typeface"] != null)
                                {
                                    string tf = tfNode.Attributes["typeface"].Value?.Trim();
                                    if (!string.IsNullOrEmpty(tf) && !tf.StartsWith("+"))
                                    {
                                        bodyFonts.Add(tf);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                // 3. Phân tích presentation.xml tìm font mapping nhúng
                Dictionary<string, string> rIdToTypeface = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                ZipArchiveEntry presEntry = archive.GetEntry("ppt/presentation.xml");
                if (presEntry != null)
                {
                    XmlDocument pDoc = new XmlDocument();
                    using (Stream s = presEntry.Open()) pDoc.Load(s);
                    XmlNodeList efList = pDoc.SelectNodes("//*[local-name()='embeddedFont']");
                    if (efList != null)
                    {
                        foreach (XmlNode ef in efList)
                        {
                            XmlNode fNode = ef.SelectSingleNode("./*[local-name()='font']");
                            string typeface = (fNode != null && fNode.Attributes["typeface"] != null) ? fNode.Attributes["typeface"].Value : "";
                            if (string.IsNullOrEmpty(typeface)) continue;

                            foreach (XmlNode rNode in ef.SelectNodes(".//*[local-name()='regular' or local-name()='bold' or local-name()='italic']"))
                            {
                                string rId = (rNode.Attributes["r:id"] != null) ? rNode.Attributes["r:id"].Value : (rNode.Attributes["id"] != null ? rNode.Attributes["id"].Value : "");
                                if (!string.IsNullOrEmpty(rId) && !rIdToTypeface.ContainsKey(rId))
                                {
                                    rIdToTypeface[rId] = typeface;
                                }
                            }
                        }
                    }
                }

                // 4. Phân tích relationships
                Dictionary<string, string> targetToTypeface = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                ZipArchiveEntry presRels = archive.GetEntry("ppt/_rels/presentation.xml.rels");
                if (presRels != null)
                {
                    XmlDocument prDoc = new XmlDocument();
                    using (Stream rs = presRels.Open()) prDoc.Load(rs);
                    foreach (var kvp in rIdToTypeface)
                    {
                        XmlNode rel = prDoc.SelectSingleNode(string.Format("//*[local-name()='Relationship'][@Id='{0}']", kvp.Key));
                        if (rel != null && rel.Attributes["Target"] != null)
                        {
                            string target = rel.Attributes["Target"].Value.Replace('\\', '/');
                            if (!target.StartsWith("ppt/")) target = "ppt/" + target;
                            targetToTypeface[target] = kvp.Value;
                        }
                    }
                }

                // 5. Đọc các tệp font nhúng trong ppt/fonts/
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string fn = entry.FullName.Replace('\\', '/');
                    if (fn.StartsWith("ppt/fonts/", StringComparison.OrdinalIgnoreCase) &&
                        (fn.EndsWith(".fntdata", StringComparison.OrdinalIgnoreCase) || fn.EndsWith(".odttf", StringComparison.OrdinalIgnoreCase) || fn.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || fn.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)))
                    {
                        byte[] raw;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            using (Stream es = entry.Open()) es.CopyTo(ms);
                            raw = ms.ToArray();
                        }

                        byte[] cleanFont = null;
                        bool isOtf = false;

                        if (fn.EndsWith(".fntdata", StringComparison.OrdinalIgnoreCase))
                        {
                            cleanFont = ExtractEotFont(raw, out isOtf);
                        }
                        else if (fn.EndsWith(".odttf", StringComparison.OrdinalIgnoreCase))
                        {
                            string guid = Path.GetFileNameWithoutExtension(fn);
                            cleanFont = DeobfuscateOdttf(raw, guid);
                        }
                        else
                        {
                            cleanFont = raw;
                        }

                        if (cleanFont != null && cleanFont.Length > 0)
                        {
                            string subfam;
                            string fam = ReadFontFamilyName(cleanFont, out subfam);
                            string mappedTf;
                            if (targetToTypeface.TryGetValue(fn, out mappedTf) && !string.IsNullOrEmpty(mappedTf))
                            {
                                fam = mappedTf;
                            }

                            List<char> missing;
                            bool viSupport = CheckFontVietnameseSupport(cleanFont, out missing);

                            string safeName = $"{fam.Replace(' ', '_')}.{(isOtf ? "otf" : "ttf")}";

                            var info = new ExtractedFontInfo
                            {
                                Typeface = fam,
                                Subfamily = subfam,
                                Format = isOtf ? "OpenType" : "TrueType",
                                FileName = safeName,
                                Data = cleanFont,
                                IsEmbedded = true,
                                IsInstalledOnSystem = IsSystemFontInstalled(fam),
                                HasVietnameseSupport = viSupport,
                                MissingVietnameseChars = missing,
                                SuggestedVietnameseFont = viSupport ? fam : GetSuggestedVietnameseFont(fam),
                                SourceDocument = pptxPath,
                                IsUsedInDocumentBody = bodyFonts.Contains(fam)
                            };

                            map[fam] = info;
                        }
                    }
                }

                // 6. Thêm các phông chữ dùng trong tài liệu nhưng không nhúng file
                foreach (string fam in bodyFonts)
                {
                    if (!map.ContainsKey(fam))
                    {
                        bool isTcvn3 = fam.StartsWith(".Vn", StringComparison.OrdinalIgnoreCase) || fam.StartsWith(".vn", StringComparison.OrdinalIgnoreCase);
                        bool isVni = fam.StartsWith("VNI-", StringComparison.OrdinalIgnoreCase) || fam.StartsWith("vni-", StringComparison.OrdinalIgnoreCase);
                        string formatStr = isTcvn3 ? "TCVN3 (ABC)" : (isVni ? "VNI-Windows" : "TrueType");

                        List<char> missing;
                        bool viSupport = CheckInstalledFontVietnameseSupport(fam, out missing);

                        map[fam] = new ExtractedFontInfo
                        {
                            Typeface = fam,
                            Subfamily = "Regular",
                            Format = formatStr,
                            FileName = $"{fam.Replace(' ', '_')}.ttf",
                            Data = null,
                            IsEmbedded = false,
                            IsInstalledOnSystem = IsSystemFontInstalled(fam),
                            HasVietnameseSupport = viSupport,
                            MissingVietnameseChars = missing,
                            SuggestedVietnameseFont = viSupport ? fam : GetSuggestedVietnameseFont(fam),
                            SourceDocument = pptxPath,
                            IsUsedInDocumentBody = true
                        };
                    }
                }
            }

            list.AddRange(map.Values);
            list.Sort((a, b) =>
            {
                int scoreA = (!a.HasVietnameseSupport ? 100 : 0) + (a.IsEmbedded ? 50 : 0) + (a.IsUsedInDocumentBody ? 25 : 0);
                int scoreB = (!b.HasVietnameseSupport ? 100 : 0) + (b.IsEmbedded ? 50 : 0) + (b.IsUsedInDocumentBody ? 25 : 0);
                if (scoreA != scoreB) return scoreB.CompareTo(scoreA);
                return string.Compare(a.Typeface, b.Typeface, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        private static List<ExtractedFontInfo> AnalyzeDocxFonts(string docxPath)
        {
            List<ExtractedFontInfo> list = new List<ExtractedFontInfo>();
            Dictionary<string, ExtractedFontInfo> map = new Dictionary<string, ExtractedFontInfo>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> bodyFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> allDeclaredFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (FileStream fs = new FileStream(docxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                // 1. Quét nội dung thực tế trong word/document.xml
                ZipArchiveEntry docEntry = archive.GetEntry("word/document.xml");
                if (docEntry != null)
                {
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        using (Stream s = docEntry.Open()) doc.Load(s);
                        XmlNodeList rFontsList = doc.SelectNodes("//*[local-name()='rFonts']");
                        if (rFontsList != null)
                        {
                            foreach (XmlNode rf in rFontsList)
                            {
                                string[] attrs = { "w:ascii", "w:hAnsi", "w:cs", "w:eastAsia" };
                                foreach (string a in attrs)
                                {
                                    if (rf.Attributes[a] != null)
                                    {
                                        string val = rf.Attributes[a].Value?.Trim();
                                        if (!string.IsNullOrEmpty(val) && !val.StartsWith("minor", StringComparison.OrdinalIgnoreCase) && !val.StartsWith("default", StringComparison.OrdinalIgnoreCase))
                                        {
                                            bodyFonts.Add(val);
                                            allDeclaredFonts.Add(val);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                // 2. Phân tích word/fontTable.xml và fontKey
                Dictionary<string, string> rIdToFontKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> rIdToTypeface = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                ZipArchiveEntry ftEntry = archive.GetEntry("word/fontTable.xml");
                if (ftEntry != null)
                {
                    XmlDocument ftDoc = new XmlDocument();
                    using (Stream s = ftEntry.Open()) ftDoc.Load(s);
                    XmlNodeList fNodes = ftDoc.SelectNodes("//*[local-name()='font']");
                    if (fNodes != null)
                    {
                        foreach (XmlNode fn in fNodes)
                        {
                            string fontName = fn.Attributes["w:name"] != null ? fn.Attributes["w:name"].Value : "";
                            if (string.IsNullOrEmpty(fontName)) continue;
                            allDeclaredFonts.Add(fontName);

                            foreach (XmlNode emb in fn.SelectNodes(".//*[local-name()='embedRegular' or local-name()='embedBold' or local-name()='embedItalic']"))
                            {
                                string rId = emb.Attributes["r:id"] != null ? emb.Attributes["r:id"].Value : "";
                                string key = emb.Attributes["w:fontKey"] != null ? emb.Attributes["w:fontKey"].Value : "";
                                if (!string.IsNullOrEmpty(rId))
                                {
                                    rIdToTypeface[rId] = fontName;
                                    if (!string.IsNullOrEmpty(key)) rIdToFontKey[rId] = key;
                                }
                            }
                        }
                    }
                }

                // 3. Quét word/styles.xml
                ZipArchiveEntry stylesEntry = archive.GetEntry("word/styles.xml");
                if (stylesEntry != null)
                {
                    try
                    {
                        XmlDocument sDoc = new XmlDocument();
                        using (Stream s = stylesEntry.Open()) sDoc.Load(s);
                        XmlNodeList rfNodes = sDoc.SelectNodes("//*[local-name()='rFonts']");
                        if (rfNodes != null)
                        {
                            foreach (XmlNode rf in rfNodes)
                            {
                                string[] attrs = { "w:ascii", "w:hAnsi", "w:cs", "w:eastAsia" };
                                foreach (string a in attrs)
                                {
                                    if (rf.Attributes[a] != null)
                                    {
                                        string val = rf.Attributes[a].Value?.Trim();
                                        if (!string.IsNullOrEmpty(val) && !val.StartsWith("minor", StringComparison.OrdinalIgnoreCase) && !val.StartsWith("default", StringComparison.OrdinalIgnoreCase))
                                        {
                                            allDeclaredFonts.Add(val);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                // 4. Phân tích relationships của fontTable
                Dictionary<string, string> targetToTypeface = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> targetToFontKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                ZipArchiveEntry ftRels = archive.GetEntry("word/_rels/fontTable.xml.rels");
                if (ftRels != null)
                {
                    XmlDocument relDoc = new XmlDocument();
                    using (Stream rs = ftRels.Open()) relDoc.Load(rs);
                    foreach (var kvp in rIdToTypeface)
                    {
                        XmlNode rel = relDoc.SelectSingleNode(string.Format("//*[local-name()='Relationship'][@Id='{0}']", kvp.Key));
                        if (rel != null && rel.Attributes["Target"] != null)
                        {
                            string target = rel.Attributes["Target"].Value.Replace('\\', '/');
                            if (!target.StartsWith("word/")) target = "word/" + target;
                            targetToTypeface[target] = kvp.Value;
                            if (rIdToFontKey.ContainsKey(kvp.Key)) targetToFontKey[target] = rIdToFontKey[kvp.Key];
                        }
                    }
                }

                // 5. Đọc các tệp font nhúng trong word/fonts/
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string fn = entry.FullName.Replace('\\', '/');
                    if (fn.StartsWith("word/fonts/", StringComparison.OrdinalIgnoreCase))
                    {
                        byte[] raw;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            using (Stream es = entry.Open()) es.CopyTo(ms);
                            raw = ms.ToArray();
                        }

                        byte[] cleanFont = null;
                        bool isOtf = false;

                        if (fn.EndsWith(".odttf", StringComparison.OrdinalIgnoreCase))
                        {
                            string key;
                            if (!targetToFontKey.TryGetValue(fn, out key) || string.IsNullOrEmpty(key))
                            {
                                key = Path.GetFileNameWithoutExtension(fn);
                            }
                            cleanFont = DeobfuscateOdttf(raw, key);
                        }
                        else if (fn.EndsWith(".fntdata", StringComparison.OrdinalIgnoreCase))
                        {
                            cleanFont = ExtractEotFont(raw, out isOtf);
                        }
                        else
                        {
                            cleanFont = raw;
                        }

                        if (cleanFont != null && cleanFont.Length > 0)
                        {
                            string subfam;
                            string fam = ReadFontFamilyName(cleanFont, out subfam);
                            string mapped;
                            if (targetToTypeface.TryGetValue(fn, out mapped) && !string.IsNullOrEmpty(mapped)) fam = mapped;

                            List<char> missing;
                            bool viSupport = CheckFontVietnameseSupport(cleanFont, out missing);

                            string safeName = $"{fam.Replace(' ', '_')}.{(isOtf ? "otf" : "ttf")}";

                            var info = new ExtractedFontInfo
                            {
                                Typeface = fam,
                                Subfamily = subfam,
                                Format = isOtf ? "OpenType" : "TrueType",
                                FileName = safeName,
                                Data = cleanFont,
                                IsEmbedded = true,
                                IsInstalledOnSystem = IsSystemFontInstalled(fam),
                                HasVietnameseSupport = viSupport,
                                MissingVietnameseChars = missing,
                                SuggestedVietnameseFont = viSupport ? fam : GetSuggestedVietnameseFont(fam),
                                SourceDocument = docxPath,
                                IsUsedInDocumentBody = bodyFonts.Contains(fam)
                            };

                            map[fam] = info;
                        }
                    }
                }

                // 6. Thêm các phông chữ dùng trong tài liệu nhưng không nhúng file
                foreach (string fam in allDeclaredFonts)
                {
                    if (!map.ContainsKey(fam))
                    {
                        bool isTcvn3 = fam.StartsWith(".Vn", StringComparison.OrdinalIgnoreCase) || fam.StartsWith(".vn", StringComparison.OrdinalIgnoreCase);
                        bool isVni = fam.StartsWith("VNI-", StringComparison.OrdinalIgnoreCase) || fam.StartsWith("vni-", StringComparison.OrdinalIgnoreCase);
                        string formatStr = isTcvn3 ? "TCVN3 (ABC)" : (isVni ? "VNI-Windows" : "TrueType");

                        List<char> missing;
                        bool viSupport = CheckInstalledFontVietnameseSupport(fam, out missing);

                        map[fam] = new ExtractedFontInfo
                        {
                            Typeface = fam,
                            Subfamily = "Regular",
                            Format = formatStr,
                            FileName = $"{fam.Replace(' ', '_')}.ttf",
                            Data = null,
                            IsEmbedded = false,
                            IsInstalledOnSystem = IsSystemFontInstalled(fam),
                            HasVietnameseSupport = viSupport,
                            MissingVietnameseChars = missing,
                            SuggestedVietnameseFont = viSupport ? fam : GetSuggestedVietnameseFont(fam),
                            SourceDocument = docxPath,
                            IsUsedInDocumentBody = bodyFonts.Contains(fam)
                        };
                    }
                }
            }

            list.AddRange(map.Values);
            list.Sort((a, b) =>
            {
                int scoreA = (!a.HasVietnameseSupport ? 100 : 0) + (a.IsEmbedded ? 50 : 0) + (a.IsUsedInDocumentBody ? 25 : 0);
                int scoreB = (!b.HasVietnameseSupport ? 100 : 0) + (b.IsEmbedded ? 50 : 0) + (b.IsUsedInDocumentBody ? 25 : 0);
                if (scoreA != scoreB) return scoreB.CompareTo(scoreA);
                return string.Compare(a.Typeface, b.Typeface, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        /// <summary>
        /// Chuyển đổi văn bản từ bảng mã TCVN3 (ABC) sang Unicode chuẩn
        /// </summary>
        public static string ConvertTcvn3ToUnicode(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            StringBuilder sb = new StringBuilder(text.Length);

            foreach (char c in text)
            {
                char uni;
                if (Tcvn3ToUnicodeMap.TryGetValue(c, out uni))
                {
                    sb.Append(uni);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Chuyển đổi văn bản từ bảng mã VNI-Windows sang Unicode chuẩn
        /// </summary>
        public static string ConvertVniToUnicode(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            StringBuilder sb = new StringBuilder(text);

            foreach (var kvp in VniToUnicodeMap)
            {
                sb.Replace(kvp.Key, kvp.Value);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Tự động phát hiện và sửa toàn diện văn bản tiếng Việt: TCVN3, VNI hoặc Unicode tổ hợp
        /// </summary>
        public static string FixVietnameseTextString(string text, FontFixOptions options)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string cur = text;

            // 1. Chuyển mã TCVN3 nếu phát hiện ký tự TCVN3 đặc trưng
            if (options.ConvertTcvn3)
            {
                bool hasTcvn3 = false;
                foreach (char c in cur)
                {
                    if (Tcvn3ToUnicodeMap.ContainsKey(c)) { hasTcvn3 = true; break; }
                }
                if (hasTcvn3) cur = ConvertTcvn3ToUnicode(cur);
            }

            // 2. Chuyển mã VNI nếu phát hiện tổ hợp VNI
            if (options.ConvertVni)
            {
                if (cur.Contains("aù") || cur.Contains("aø") || cur.Contains("aû") || cur.Contains("eù") || cur.Contains("uù") || cur.Contains("ñ"))
                {
                    cur = ConvertVniToUnicode(cur);
                }
            }

            // 3. Chuẩn hóa sang Unicode Dựng Sẵn (NFC)
            if (options.NormalizeNfc)
            {
                cur = cur.Normalize(NormalizationForm.FormC);
            }

            return cur;
        }

        /// <summary>
        /// Trích xuất toàn bộ các phông chữ ra thư mục chỉ định
        /// </summary>
        public static bool ExportFonts(List<ExtractedFontInfo> fonts, string targetFolder, out string message)
        {
            message = "";
            if (fonts == null || fonts.Count == 0)
            {
                message = "Không có phông chữ nào để trích xuất.";
                return false;
            }

            if (!Directory.Exists(targetFolder))
            {
                try { Directory.CreateDirectory(targetFolder); } catch (Exception ex) { message = ex.Message; return false; }
            }

            int count = 0;
            foreach (var f in fonts)
            {
                if (f.Data == null || f.Data.Length == 0) continue;
                string cleanName = f.FileName;
                if (string.IsNullOrEmpty(cleanName)) cleanName = $"{f.Typeface.Replace(' ', '_')}.ttf";

                string outPath = Path.Combine(targetFolder, cleanName);
                try
                {
                    File.WriteAllBytes(outPath, f.Data);
                    count++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error writing font: " + ex.Message);
                }
            }

            message = $"Đã trích xuất thành công {count} tệp phông chữ vào thư mục:\n{targetFolder}";
            return count > 0;
        }

        /// <summary>
        /// Cài đặt phông chữ nhúng vào thư mục ZenQ Office và đăng ký runtime
        /// </summary>
        public static bool InstallFontToApp(ExtractedFontInfo font, out string message)
        {
            message = "";
            if (font == null || font.Data == null)
            {
                message = "Dữ liệu phông chữ không hợp lệ.";
                return false;
            }

            try
            {
                string fontsDir = SlideFontManager.GetAppFontsDirectory();
                string safeName = font.FileName;
                if (string.IsNullOrEmpty(safeName)) safeName = $"{font.Typeface.Replace(' ', '_')}.ttf";
                string targetPath = Path.Combine(fontsDir, safeName);

                File.WriteAllBytes(targetPath, font.Data);
                string importedName;
                string err;
                if (SlideFontManager.ImportCustomFont(targetPath, out importedName, out err))
                {
                    message = $"Đã nạp thành công phông chữ '{font.Typeface}' vào ZenQ Office!";
                    return true;
                }
                else
                {
                    message = err ?? "Không thể đăng ký phông chữ.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Sửa lỗi Tiếng Việt và thay thế phông chữ thiếu tiếng Việt trong tệp PPTX
        /// </summary>
        public static DocumentFixReport FixPresentationVietnamese(string pptxPath, string outPath, FontFixOptions options)
        {
            var report = new DocumentFixReport { OutputFilePath = outPath };
            if (!File.Exists(pptxPath))
            {
                report.Message = "Tệp PPTX không tồn tại.";
                return report;
            }

            try
            {
                // Phân tích danh sách font nhúng và độ phủ tiếng Việt
                var fontList = AnalyzeDocumentFonts(pptxPath);
                Dictionary<string, string> replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var f in fontList)
                {
                    if (!f.HasVietnameseSupport && options.ReplaceUnsupportedFonts)
                    {
                        string target = options.CustomFontReplacements.ContainsKey(f.Typeface)
                            ? options.CustomFontReplacements[f.Typeface]
                            : f.SuggestedVietnameseFont;

                        if (!string.IsNullOrEmpty(target) && !string.Equals(f.Typeface, target, StringComparison.OrdinalIgnoreCase))
                        {
                            replacements[f.Typeface] = target;
                            report.ReplacedFonts.Add($"{f.Typeface} ➔ {target}");
                        }
                    }
                }

                // Đọc file PPTX và sửa trực tiếp các slide XML
                byte[] pptxBytes = File.ReadAllBytes(pptxPath);
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(pptxBytes, 0, pptxBytes.Length);
                    using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Update, true))
                    {
                        foreach (var entry in new List<ZipArchiveEntry>(zip.Entries))
                        {
                            string fn = entry.FullName.Replace('\\', '/');
                            if (fn.StartsWith("ppt/slides/slide") && fn.EndsWith(".xml"))
                            {
                                XmlDocument sDoc = new XmlDocument();
                                using (Stream s = entry.Open()) sDoc.Load(s);

                                bool modified = false;

                                // 1. Sửa văn bản trong thẻ <a:t>
                                XmlNodeList tNodes = sDoc.SelectNodes("//*[local-name()='t']");
                                if (tNodes != null)
                                {
                                    foreach (XmlNode t in tNodes)
                                    {
                                        string oldT = t.InnerText;
                                        if (!string.IsNullOrEmpty(oldT))
                                        {
                                            string newT = FixVietnameseTextString(oldT, options);
                                            if (newT != oldT)
                                            {
                                                t.InnerText = newT;
                                                report.TextChangesCount++;
                                                modified = true;
                                            }
                                        }
                                    }
                                }

                                // 2. Thay thế font không hỗ trợ tiếng Việt trong <a:rPr> / <a:latin> / <a:ea> / <a:cs>
                                if (replacements.Count > 0)
                                {
                                    XmlNodeList rPrNodes = sDoc.SelectNodes("//*[local-name()='latin' or local-name()='ea' or local-name()='cs' or local-name()='sym']");
                                    if (rPrNodes != null)
                                    {
                                        foreach (XmlNode fontTag in rPrNodes)
                                        {
                                            if (fontTag.Attributes["typeface"] != null)
                                            {
                                                string oldTf = fontTag.Attributes["typeface"].Value;
                                                string newTf;
                                                if (replacements.TryGetValue(oldTf, out newTf))
                                                {
                                                    fontTag.Attributes["typeface"].Value = newTf;
                                                    report.FontsReplacedCount++;
                                                    modified = true;
                                                }
                                            }
                                        }
                                    }
                                }

                                report.TotalItemsProcessed++;

                                if (modified)
                                {
                                    entry.Delete();
                                    ZipArchiveEntry newEntry = zip.CreateEntry(fn, CompressionLevel.Optimal);
                                    using (Stream s = newEntry.Open())
                                    {
                                        sDoc.Save(s);
                                    }
                                }
                            }
                        }
                    }

                    File.WriteAllBytes(outPath, ms.ToArray());
                }

                report.Success = true;
                report.Message = $"Đã sửa lỗi thành công! Đã sửa {report.TextChangesCount} chuỗi văn bản và thay thế {report.FontsReplacedCount} lượt phông chữ.";
            }
            catch (Exception ex)
            {
                report.Success = false;
                report.Message = "Lỗi khi xử lý PPTX: " + ex.Message;
            }

            return report;
        }

        /// <summary>
        /// Sửa lỗi Tiếng Việt và thay thế phông chữ thiếu tiếng Việt trong tệp Word DOCX
        /// </summary>
        public static DocumentFixReport FixDocxVietnamese(string docxPath, string outPath, FontFixOptions options)
        {
            var report = new DocumentFixReport { OutputFilePath = outPath };
            if (!File.Exists(docxPath))
            {
                report.Message = "Tệp DOCX không tồn tại.";
                return report;
            }

            try
            {
                var fontList = AnalyzeDocumentFonts(docxPath);
                Dictionary<string, string> replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var f in fontList)
                {
                    if (!f.HasVietnameseSupport && options.ReplaceUnsupportedFonts)
                    {
                        string target = options.CustomFontReplacements.ContainsKey(f.Typeface)
                            ? options.CustomFontReplacements[f.Typeface]
                            : f.SuggestedVietnameseFont;

                        if (!string.IsNullOrEmpty(target) && !string.Equals(f.Typeface, target, StringComparison.OrdinalIgnoreCase))
                        {
                            replacements[f.Typeface] = target;
                            report.ReplacedFonts.Add($"{f.Typeface} ➔ {target}");
                        }
                    }
                }

                byte[] docxBytes = File.ReadAllBytes(docxPath);
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(docxBytes, 0, docxBytes.Length);
                    using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Update, true))
                    {
                        foreach (var entry in new List<ZipArchiveEntry>(zip.Entries))
                        {
                            string fn = entry.FullName.Replace('\\', '/');
                            if (fn.Equals("word/document.xml", StringComparison.OrdinalIgnoreCase) ||
                                fn.StartsWith("word/header", StringComparison.OrdinalIgnoreCase) ||
                                fn.StartsWith("word/footer", StringComparison.OrdinalIgnoreCase) ||
                                fn.Equals("word/styles.xml", StringComparison.OrdinalIgnoreCase) ||
                                fn.Equals("word/fontTable.xml", StringComparison.OrdinalIgnoreCase))
                            {
                                XmlDocument doc = new XmlDocument();
                                using (Stream s = entry.Open()) doc.Load(s);

                                bool modified = false;

                                // 1. Sửa văn bản trong thẻ <w:t>
                                XmlNodeList tNodes = doc.SelectNodes("//*[local-name()='t']");
                                if (tNodes != null)
                                {
                                    foreach (XmlNode t in tNodes)
                                    {
                                        string oldT = t.InnerText;
                                        if (!string.IsNullOrEmpty(oldT))
                                        {
                                            string newT = FixVietnameseTextString(oldT, options);
                                            if (newT != oldT)
                                            {
                                                t.InnerText = newT;
                                                report.TextChangesCount++;
                                                modified = true;
                                            }
                                        }
                                    }
                                }

                                // 2. Thay thế font trong <w:rFonts>
                                if (replacements.Count > 0)
                                {
                                    XmlNodeList rFontsList = doc.SelectNodes("//*[local-name()='rFonts']");
                                    if (rFontsList != null)
                                    {
                                        foreach (XmlNode rf in rFontsList)
                                        {
                                            string[] attrs = { "w:ascii", "w:hAnsi", "w:cs", "w:eastAsia" };
                                            foreach (string a in attrs)
                                            {
                                                if (rf.Attributes[a] != null)
                                                {
                                                    string oldF = rf.Attributes[a].Value;
                                                    string newF;
                                                    if (replacements.TryGetValue(oldF, out newF))
                                                    {
                                                        rf.Attributes[a].Value = newF;
                                                        report.FontsReplacedCount++;
                                                        modified = true;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    // Cập nhật tên font trong word/fontTable.xml
                                    XmlNodeList fNodes = doc.SelectNodes("//*[local-name()='font']");
                                    if (fNodes != null)
                                    {
                                        foreach (XmlNode fnNode in fNodes)
                                        {
                                            if (fnNode.Attributes["w:name"] != null)
                                            {
                                                string oldF = fnNode.Attributes["w:name"].Value;
                                                string newF;
                                                if (replacements.TryGetValue(oldF, out newF))
                                                {
                                                    fnNode.Attributes["w:name"].Value = newF;
                                                    modified = true;
                                                }
                                            }
                                        }
                                    }
                                }

                                report.TotalItemsProcessed++;

                                if (modified)
                                {
                                    entry.Delete();
                                    ZipArchiveEntry newEntry = zip.CreateEntry(fn, CompressionLevel.Optimal);
                                    using (Stream s = newEntry.Open())
                                    {
                                        doc.Save(s);
                                    }
                                }
                            }
                        }
                    }

                    File.WriteAllBytes(outPath, ms.ToArray());
                }

                report.Success = true;
                report.Message = $"Đã sửa lỗi thành công! Đã sửa {report.TextChangesCount} chuỗi văn bản và thay thế {report.FontsReplacedCount} lượt phông chữ.";
            }
            catch (Exception ex)
            {
                report.Success = false;
                report.Message = "Lỗi khi xử lý DOCX: " + ex.Message;
            }

            return report;
        }
    }
}

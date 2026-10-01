using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GenOffice.Core
{
    /// <summary>
    /// Trình phân tích và đóng gói JSON thuần C# siêu nhẹ, tốc độ cao, không phụ thuộc thư viện ngoài (.NET 4.0 compatible).
    /// Hỗ trợ đầy đủ Object, Array, String, Number, Boolean, Null và các ký tự Unicode/Escape.
    /// </summary>
    public static class JsonLite
    {
        public static string Serialize(object obj)
        {
            if (obj == null) return "null";

            if (obj is string)
            {
                return "\"" + EscapeString((string)obj) + "\"";
            }
            if (obj is bool)
            {
                return ((bool)obj) ? "true" : "false";
            }
            if (obj is int || obj is long || obj is short || obj is byte || obj is uint || obj is ulong)
            {
                return Convert.ToString(obj, CultureInfo.InvariantCulture);
            }
            if (obj is float || obj is double || obj is decimal)
            {
                return Convert.ToString(obj, CultureInfo.InvariantCulture);
            }
            if (obj is IDictionary<string, object>)
            {
                var dict = (IDictionary<string, object>)obj;
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                bool first = true;
                foreach (var kvp in dict)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append("\"").Append(EscapeString(kvp.Key)).Append("\":");
                    sb.Append(Serialize(kvp.Value));
                }
                sb.Append("}");
                return sb.ToString();
            }
            if (obj is IDictionary)
            {
                var dict = (IDictionary)obj;
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                bool first = true;
                foreach (DictionaryEntry de in dict)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append("\"").Append(EscapeString(de.Key.ToString())).Append("\":");
                    sb.Append(Serialize(de.Value));
                }
                sb.Append("}");
                return sb.ToString();
            }
            if (obj is IEnumerable)
            {
                var list = (IEnumerable)obj;
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                bool first = true;
                foreach (var item in list)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append(Serialize(item));
                }
                sb.Append("]");
                return sb.ToString();
            }

            return "\"" + EscapeString(obj.ToString()) + "\"";
        }

        public static string EscapeString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            StringBuilder sb = new StringBuilder(str.Length + 10);
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                switch (c)
                {
                    case '\"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                        {
                            sb.AppendFormat("\\u{0:x4}", (int)c);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        public static object Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            int index = 0;
            return ParseValue(json, ref index);
        }

        private static void SkipWhiteSpace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }

        private static object ParseValue(string json, ref int index)
        {
            SkipWhiteSpace(json, ref index);
            if (index >= json.Length) return null;

            char c = json[index];
            if (c == '{') return ParseObject(json, ref index);
            if (c == '[') return ParseArray(json, ref index);
            if (c == '\"') return ParseString(json, ref index);
            if (c == 't' || c == 'T' || c == 'f' || c == 'F') return ParseBoolean(json, ref index);
            if (c == 'n' || c == 'N') return ParseNull(json, ref index);
            if (char.IsDigit(c) || c == '-') return ParseNumber(json, ref index);

            return null;
        }

        private static Dictionary<string, object> ParseObject(string json, ref int index)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            index++; // skip '{'

            while (index < json.Length)
            {
                SkipWhiteSpace(json, ref index);
                if (index >= json.Length) break;
                if (json[index] == '}')
                {
                    index++;
                    return dict;
                }

                string key = ParseString(json, ref index);
                SkipWhiteSpace(json, ref index);

                if (index < json.Length && json[index] == ':')
                {
                    index++;
                }

                object val = ParseValue(json, ref index);
                dict[key] = val;

                SkipWhiteSpace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                }
            }

            return dict;
        }

        private static List<object> ParseArray(string json, ref int index)
        {
            var list = new List<object>();
            index++; // skip '['

            while (index < json.Length)
            {
                SkipWhiteSpace(json, ref index);
                if (index >= json.Length) break;
                if (json[index] == ']')
                {
                    index++;
                    return list;
                }

                object val = ParseValue(json, ref index);
                list.Add(val);

                SkipWhiteSpace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                }
            }

            return list;
        }

        private static string ParseString(string json, ref int index)
        {
            SkipWhiteSpace(json, ref index);
            if (index >= json.Length || json[index] != '\"') return "";
            index++; // skip opening quote

            StringBuilder sb = new StringBuilder();
            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '\"')
                {
                    return sb.ToString();
                }
                if (c == '\\' && index < json.Length)
                {
                    char esc = json[index++];
                    switch (esc)
                    {
                        case '\"': sb.Append('\"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (index + 4 <= json.Length)
                            {
                                string hex = json.Substring(index, 4);
                                index += 4;
                                int code;
                                if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                                {
                                    sb.Append((char)code);
                                }
                            }
                            break;
                        default:
                            sb.Append(esc);
                            break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static bool ParseBoolean(string json, ref int index)
        {
            SkipWhiteSpace(json, ref index);
            if (json.Length - index >= 4 && json.Substring(index, 4).Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                index += 4;
                return true;
            }
            if (json.Length - index >= 5 && json.Substring(index, 5).Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                index += 5;
                return false;
            }
            index++;
            return false;
        }

        private static object ParseNull(string json, ref int index)
        {
            SkipWhiteSpace(json, ref index);
            if (json.Length - index >= 4 && json.Substring(index, 4).Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                index += 4;
            }
            return null;
        }

        private static object ParseNumber(string json, ref int index)
        {
            SkipWhiteSpace(json, ref index);
            int start = index;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '-' || json[index] == '+' || json[index] == '.' || json[index] == 'e' || json[index] == 'E'))
            {
                index++;
            }
            string numStr = json.Substring(start, index - start);
            if (numStr.Contains(".") || numStr.Contains("e") || numStr.Contains("E"))
            {
                double dVal;
                if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out dVal))
                {
                    return dVal;
                }
            }
            else
            {
                long lVal;
                if (long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out lVal))
                {
                    return lVal;
                }
            }
            return 0;
        }

        public static string GetString(Dictionary<string, object> dict, string key, string fallback = "")
        {
            if (dict == null || !dict.ContainsKey(key) || dict[key] == null) return fallback;
            return dict[key].ToString();
        }

        public static Dictionary<string, object> GetObject(Dictionary<string, object> dict, string key)
        {
            if (dict == null || !dict.ContainsKey(key)) return null;
            return dict[key] as Dictionary<string, object>;
        }

        public static List<object> GetArray(Dictionary<string, object> dict, string key)
        {
            if (dict == null || !dict.ContainsKey(key)) return null;
            return dict[key] as List<object>;
        }
    }
}

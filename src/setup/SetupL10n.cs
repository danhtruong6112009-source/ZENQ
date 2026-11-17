#if SETUP_STANDALONE
using System;
using System.Globalization;
using System.IO;

namespace ZenQOffice.Core
{
    internal static class AppL10n
    {
        private static readonly bool _isEnglish = DetectEnglish();

        private static bool DetectEnglish()
        {
            try
            {
                string settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ZenQOffice",
                    "settings.json");
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    if (json.IndexOf("\"en\"", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        json.IndexOf("Language", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                    if (json.IndexOf("\"vi\"", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        json.IndexOf("Language", StringComparison.OrdinalIgnoreCase) >= 0)
                        return false;
                }
            }
            catch { }
            try
            {
                string iso = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                return !string.Equals(iso, "vi", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static string T(string vi, string en)
        {
            return _isEnglish ? (en ?? vi ?? "") : (vi ?? en ?? "");
        }
    }
}
#endif

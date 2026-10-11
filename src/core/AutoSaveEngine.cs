using System;
using System.IO;

namespace ZenQOffice.Core
{
    public static class AutoSaveEngine
    {
        private static string autoSaveDir;

        static AutoSaveEngine()
        {
            try
            {
                autoSaveDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenOffice", "AutoSave");
                if (!Directory.Exists(autoSaveDir))
                {
                    Directory.CreateDirectory(autoSaveDir);
                }
            }
            catch { }
        }

        public static string AutoSavePath
        {
            get { return Path.Combine(autoSaveDir, "autosave_writer.docx"); }
        }

        public static string CrashSavePath
        {
            get { return Path.Combine(autoSaveDir, "crash_recovery_writer.docx"); }
        }

        public static void SaveSnapshot(string text)
        {
            try
            {
                if (!string.IsNullOrEmpty(autoSaveDir))
                {
                    File.WriteAllText(AutoSavePath, text);
                }
            }
            catch { }
        }

        public static void SaveCrashDump(string text)
        {
            try
            {
                if (!string.IsNullOrEmpty(autoSaveDir))
                {
                    File.WriteAllText(CrashSavePath, text);
                }
            }
            catch { }
        }

        public static bool CheckAndRecover(out string recoveredText)
        {
            recoveredText = "";
            try
            {
                string targetPath = File.Exists(CrashSavePath) ? CrashSavePath : (File.Exists(AutoSavePath) ? AutoSavePath : null);

                if (targetPath != null)
                {
                    recoveredText = File.ReadAllText(targetPath);
                    return !string.IsNullOrEmpty(recoveredText);
                }
            }
            catch { }
            return false;
        }

        public static void ClearAutoSave()
        {
            try
            {
                if (File.Exists(AutoSavePath)) File.Delete(AutoSavePath);
                if (File.Exists(CrashSavePath)) File.Delete(CrashSavePath);
            }
            catch { }
        }
    }
}

using System;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace ZenQOffice.Setup
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool isUninstall = false;
            bool isSilent = false;

            string exeName = Path.GetFileNameWithoutExtension(Application.ExecutablePath).ToLower();
            if (exeName.Contains("uninstall"))
            {
                isUninstall = true;
            }

            if (args != null)
            {
                foreach (string arg in args)
                {
                    string a = arg.ToLower().TrimStart('-', '/');
                    if (a == "uninstall" || a == "u") isUninstall = true;
                    if (a == "silent" || a == "s") isSilent = true;
                }
            }

            if (isUninstall)
            {
                if (isSilent)
                {
                    try
                    {
                        ShortcutHelper.RemoveShortcuts();
                        ShortcutHelper.UnregisterUninstall();
                    }
                    catch { }
                }
                else
                {
                    Application.Run(new UninstallForm());
                }
            }
            else
            {
                if (isSilent)
                {
                    try
                    {
                        string targetDir = ShortcutHelper.DefaultInstallPath;
                        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                        // Extract payload.zip in silent mode
                        System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                        using (Stream payloadStream = asm.GetManifestResourceStream("payload.zip"))
                        {
                            if (payloadStream != null)
                            {
                                using (System.IO.Compression.ZipArchive zip = new System.IO.Compression.ZipArchive(payloadStream, System.IO.Compression.ZipArchiveMode.Read))
                                {
                                    foreach (System.IO.Compression.ZipArchiveEntry entry in zip.Entries)
                                    {
                                        string destPath = Path.Combine(targetDir, entry.FullName);
                                        string destFolder = Path.GetDirectoryName(destPath);
                                        if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

                                        if (!string.IsNullOrEmpty(entry.Name))
                                        {
                                            entry.ExtractToFile(destPath, true);
                                        }
                                    }
                                }
                            }
                        }

                        ShortcutHelper.CreateShortcuts(targetDir);
                        ShortcutHelper.RegisterUninstall(targetDir);
                    }
                    catch { }
                }
                else
                {
                    Application.Run(new SetupForm());
                }
            }
        }
    }
}

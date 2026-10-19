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
                        foreach (var p in System.Diagnostics.Process.GetProcessesByName("ZenQOffice"))
                        {
                            try { p.Kill(); p.WaitForExit(1000); } catch { }
                        }
                        foreach (var p in System.Diagnostics.Process.GetProcessesByName("GenOffice"))
                        {
                            try { p.Kill(); p.WaitForExit(1000); } catch { }
                        }
                        ShortcutHelper.RemoveShortcuts();
                        ShortcutHelper.UnregisterUninstall();
                        ShortcutHelper.RemoveUserData();

                        string installDir = AppDomain.CurrentDomain.BaseDirectory;
                        if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir) || installDir.TrimEnd('\\', '/').Equals(Path.GetTempPath().TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase))
                        {
                            installDir = ShortcutHelper.DefaultInstallPath;
                        }

                        string tempBat = Path.Combine(Path.GetTempPath(), "cleanup_ZenQOffice.bat");
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        sb.AppendLine("@echo off");
                        sb.AppendLine("timeout /t 1 /nobreak >nul");
                        sb.AppendLine(":wait_del");
                        sb.AppendLine(string.Format("rmdir /s /q \"{0}\" >nul 2>&1", installDir));
                        if (!string.Equals(installDir, ShortcutHelper.DefaultInstallPath, StringComparison.OrdinalIgnoreCase))
                        {
                            sb.AppendLine(string.Format("rmdir /s /q \"{0}\" >nul 2>&1", ShortcutHelper.DefaultInstallPath));
                        }
                        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        sb.AppendLine(string.Format("rmdir /s /q \"{0}\" >nul 2>&1", Path.Combine(appData, "GenOffice")));
                        sb.AppendLine(string.Format("rmdir /s /q \"{0}\" >nul 2>&1", Path.Combine(appData, "ZenQOffice")));
                        sb.AppendLine(string.Format("rmdir /s /q \"{0}\" >nul 2>&1", Path.Combine(localAppData, "GenOfficeDesktop")));
                        sb.AppendLine(string.Format("rmdir /s /q \"{0}\" >nul 2>&1", Path.Combine(localAppData, "ZenQOffice")));
                        sb.AppendLine(string.Format("if exist \"{0}\" (", installDir));
                        sb.AppendLine("    timeout /t 1 /nobreak >nul");
                        sb.AppendLine("    goto wait_del");
                        sb.AppendLine(")");
                        sb.AppendLine("(goto) 2>nul & del \"%~f0\"");
                        File.WriteAllText(tempBat, sb.ToString(), System.Text.Encoding.Default);

                        System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
                        psi.FileName = "cmd.exe";
                        psi.Arguments = string.Format("/c \"{0}\"", tempBat);
                        psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        psi.CreateNoWindow = true;
                        psi.UseShellExecute = true;
                        System.Diagnostics.Process.Start(psi);
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

                        // Terminate any running ZenQOffice / GenOffice instances to unlock binaries
                        try
                        {
                            foreach (var p in System.Diagnostics.Process.GetProcessesByName("ZenQOffice"))
                            {
                                try { p.Kill(); p.WaitForExit(1500); } catch { }
                            }
                            foreach (var p in System.Diagnostics.Process.GetProcessesByName("GenOffice"))
                            {
                                try { p.Kill(); p.WaitForExit(1500); } catch { }
                            }
                        }
                        catch { }

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

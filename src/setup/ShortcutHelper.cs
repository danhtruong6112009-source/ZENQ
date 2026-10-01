using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ZenQOffice.Setup
{
    public static class ShortcutHelper
    {
        public static string DefaultInstallPath
        {
            get
            {
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localApp, "Programs", "ZenQOffice");
            }
        }

        public static void CreateShortcut(string targetExePath, string shortcutPath, string description, string iconPath = null)
        {
            try
            {
                string dir = Path.GetDirectoryName(shortcutPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // Use Windows Script Host via dynamic COM to avoid compile-time dependencies
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetExePath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(targetExePath);
                    shortcut.Description = description;
                    if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                    {
                        shortcut.IconLocation = iconPath + ",0";
                    }
                    else
                    {
                        shortcut.IconLocation = targetExePath + ",0";
                    }
                    shortcut.Save();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreateShortcut error: " + ex.Message);
            }
        }

        public static void CreateShortcuts(string installDir)
        {
            string exePath = Path.Combine(installDir, "ZenQOffice.exe");
            string icoPath = Path.Combine(installDir, "ZenQOffice.ico");

            // 1. Desktop Shortcut
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string desktopLnk = Path.Combine(desktop, "ZenQOffice.lnk");
            CreateShortcut(exePath, desktopLnk, "ZenQOffice", icoPath);

            // 2. Start Menu Shortcut
            string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
            string startMenuLnk = Path.Combine(startMenu, "ZenQOffice.lnk");
            CreateShortcut(exePath, startMenuLnk, "ZenQOffice", icoPath);
        }

        public static void RemoveShortcuts()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string desktopLnk = Path.Combine(desktop, "ZenQOffice.lnk");
                if (File.Exists(desktopLnk)) File.Delete(desktopLnk);
                string oldDesktopLnk = Path.Combine(desktop, "ZenQOffice.lnk");
                if (File.Exists(oldDesktopLnk)) File.Delete(oldDesktopLnk);

                string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                string startMenuLnk = Path.Combine(startMenu, "ZenQOffice.lnk");
                if (File.Exists(startMenuLnk)) File.Delete(startMenuLnk);
                string oldStartMenuLnk = Path.Combine(startMenu, "ZenQOffice.lnk");
                if (File.Exists(oldStartMenuLnk)) File.Delete(oldStartMenuLnk);
            }
            catch { }
        }

        public static void RegisterUninstall(string installDir)
        {
            try
            {
                string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\ZenQOffice";
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    if (key != null)
                    {
                        string exePath = Path.Combine(installDir, "ZenQOffice.exe");
                        string uninstallExe = Path.Combine(installDir, "Uninstall.exe");
                        string icoPath = Path.Combine(installDir, "ZenQOffice.ico");

                        key.SetValue("DisplayName", "ZENQ OFFICE");
                        key.SetValue("DisplayVersion", "3.5");
                        key.SetValue("Publisher", "NEXTGENEDUSPACE STUDIO");
                        key.SetValue("DisplayIcon", File.Exists(icoPath) ? icoPath : exePath);
                        key.SetValue("InstallLocation", installDir);
                        key.SetValue("UninstallString", string.Format("\"{0}\"", uninstallExe));
                        key.SetValue("QuietUninstallString", string.Format("\"{0}\" /silent", uninstallExe));
                        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("RegisterUninstall error: " + ex.Message);
            }
        }

        public static void UnregisterUninstall()
        {
            try
            {
                string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
                using (RegistryKey parent = Registry.CurrentUser.OpenSubKey(keyPath, true))
                {
                    if (parent != null)
                    {
                        parent.DeleteSubKeyTree("ZenQOffice", false);
                        parent.DeleteSubKeyTree("ZenQOffice", false);
                    }
                }
            }
            catch { }
        }
    }
}

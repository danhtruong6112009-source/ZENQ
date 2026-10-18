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

        public static void CreateShortcut(string targetExePath, string shortcutPath, string description, string iconPath = null, string arguments = null)
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
                    if (!string.IsNullOrEmpty(arguments)) shortcut.Arguments = arguments;
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

        public static void CleanObsoleteShortcuts()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

                string[] obsoleteNames = { "ZenQOffice.lnk", "ZenQ Office Suite.lnk", "GenOffice.lnk", "GenOffice Suite.lnk", "GenOffice Writer.lnk", "GenOffice Calc.lnk", "GenOffice Slides.lnk", "GenOffice PDF.lnk" };
                foreach (string oldName in obsoleteNames)
                {
                    string dPath = Path.Combine(desktop, oldName);
                    if (File.Exists(dPath)) { try { File.Delete(dPath); } catch { } }

                    string sPath = Path.Combine(startMenu, oldName);
                    if (File.Exists(sPath)) { try { File.Delete(sPath); } catch { } }
                }
            }
            catch { }
        }

        public static void CreateShortcuts(string installDir)
        {
            CleanObsoleteShortcuts();

            string exePath = Path.Combine(installDir, "ZenQOffice.exe");
            string icoPath = Path.Combine(installDir, "ZenQOffice.ico");
            string iconDir = Path.Combine(installDir, "icon");
            string writerIco = Path.Combine(iconDir, "writer.ico");
            string calcIco = Path.Combine(iconDir, "calc.ico");
            string slidesIco = Path.Combine(iconDir, "present.ico");
            string pdfIco = Path.Combine(iconDir, "pdf.ico");

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

            // 1. Tạo các lối tắt App Nhánh độc lập trên Desktop (Mở app nhánh, không chia tab, không mở app mẹ)
            CreateShortcut(exePath, Path.Combine(desktop, "ZenQ Writer.lnk"), "ZenQ Writer - Soạn thảo văn bản độc lập", File.Exists(writerIco) ? writerIco : icoPath, "-word");
            CreateShortcut(exePath, Path.Combine(desktop, "ZenQ Calc.lnk"), "ZenQ Calc - Bảng tính điện tử độc lập", File.Exists(calcIco) ? calcIco : icoPath, "-calc");
            CreateShortcut(exePath, Path.Combine(desktop, "ZenQ Slides.lnk"), "ZenQ Slides - Thiết kế trình chiếu độc lập", File.Exists(slidesIco) ? slidesIco : icoPath, "-slides");
            CreateShortcut(exePath, Path.Combine(desktop, "ZenQ PDF.lnk"), "ZenQ PDF - Trình đọc & biên tập PDF", File.Exists(pdfIco) ? pdfIco : icoPath, "-pdf");
            CreateShortcut(exePath, Path.Combine(desktop, "ZENQ OFFICE.lnk"), "ZENQ OFFICE", icoPath, "");

            // 2. Start Menu Shortcuts
            CreateShortcut(exePath, Path.Combine(startMenu, "ZenQ Writer.lnk"), "ZenQ Writer", File.Exists(writerIco) ? writerIco : icoPath, "-word");
            CreateShortcut(exePath, Path.Combine(startMenu, "ZenQ Calc.lnk"), "ZenQ Calc", File.Exists(calcIco) ? calcIco : icoPath, "-calc");
            CreateShortcut(exePath, Path.Combine(startMenu, "ZenQ Slides.lnk"), "ZenQ Slides", File.Exists(slidesIco) ? slidesIco : icoPath, "-slides");
            CreateShortcut(exePath, Path.Combine(startMenu, "ZenQ PDF.lnk"), "ZenQ PDF", File.Exists(pdfIco) ? pdfIco : icoPath, "-pdf");
            CreateShortcut(exePath, Path.Combine(startMenu, "ZENQ OFFICE.lnk"), "ZENQ OFFICE", icoPath, "");

            // 3. Đăng ký nhận diện tệp với Windows Registry (File Associations)
            RegisterFileAssociations(installDir);
        }

        public static void RegisterFileAssociations(string installDir)
        {
            try
            {
                string exePath = Path.Combine(installDir, "ZenQOffice.exe");
                string iconDir = Path.Combine(installDir, "icon");
                string mainIco = Path.Combine(installDir, "ZenQOffice.ico");

                var types = new[]
                {
                    new { Ext = ".docx", ProgId = "ZenQOffice.Docx", Desc = "Tài liệu Microsoft Word (.docx)", Param = "-word", Icon = "writer.ico" },
                    new { Ext = ".doc", ProgId = "ZenQOffice.Doc", Desc = "Tài liệu Word 97-2003 (.doc)", Param = "-word", Icon = "writer.ico" },
                    new { Ext = ".rtf", ProgId = "ZenQOffice.Rtf", Desc = "Tài liệu Rich Text (.rtf)", Param = "-word", Icon = "writer.ico" },
                    new { Ext = ".txt", ProgId = "ZenQOffice.Txt", Desc = "Tài liệu Văn bản (.txt)", Param = "-word", Icon = "writer.ico" },
                    new { Ext = ".xlsx", ProgId = "ZenQOffice.Xlsx", Desc = "Bảng tính Microsoft Excel (.xlsx)", Param = "-calc", Icon = "calc.ico" },
                    new { Ext = ".xls", ProgId = "ZenQOffice.Xls", Desc = "Bảng tính Excel 97-2003 (.xls)", Param = "-calc", Icon = "calc.ico" },
                    new { Ext = ".csv", ProgId = "ZenQOffice.Csv", Desc = "Tệp dữ liệu CSV (.csv)", Param = "-calc", Icon = "calc.ico" },
                    new { Ext = ".pptx", ProgId = "ZenQOffice.Pptx", Desc = "Bài trình chiếu PowerPoint (.pptx)", Param = "-slides", Icon = "present.ico" },
                    new { Ext = ".ppt", ProgId = "ZenQOffice.Ppt", Desc = "Bài trình chiếu PowerPoint 97-2003 (.ppt)", Param = "-slides", Icon = "present.ico" },
                    new { Ext = ".pdf", ProgId = "ZenQOffice.Pdf", Desc = "Tài liệu Adobe PDF (.pdf)", Param = "-pdf", Icon = "pdf.ico" }
                };

                foreach (var t in types)
                {
                    string ico = Path.Combine(iconDir, t.Icon);
                    if (!File.Exists(ico)) ico = mainIco;

                    using (RegistryKey pk = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + t.ProgId))
                    {
                        if (pk != null)
                        {
                            pk.SetValue("", t.Desc);
                            pk.SetValue("FriendlyTypeName", t.Desc);
                            using (RegistryKey ik = pk.CreateSubKey("DefaultIcon")) { if (ik != null) ik.SetValue("", ico + ",0"); }
                            using (RegistryKey ck = pk.CreateSubKey(@"shell\open\command")) { if (ck != null) ck.SetValue("", "\"" + exePath + "\" " + t.Param + " \"%1\""); }
                        }
                    }

                    using (RegistryKey ek = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + t.Ext))
                    {
                        if (ek != null)
                        {
                            ek.SetValue("", t.ProgId);
                            using (RegistryKey ow = ek.CreateSubKey("OpenWithProgids")) { if (ow != null) ow.SetValue(t.ProgId, ""); }
                        }
                    }
                }

                using (RegistryKey reg = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))
                {
                    if (reg != null) reg.SetValue("ZenQOffice", @"Software\ZenQOffice\Capabilities");
                }

                using (RegistryKey cap = Registry.CurrentUser.CreateSubKey(@"Software\ZenQOffice\Capabilities"))
                {
                    if (cap != null)
                    {
                        cap.SetValue("ApplicationName", "ZenQ Office Suite");
                        cap.SetValue("ApplicationDescription", "Bộ ứng dụng văn phòng thế hệ mới ZenQ Office");
                        using (RegistryKey fa = cap.CreateSubKey("FileAssociations"))
                        {
                            if (fa != null)
                            {
                                foreach (var t in types) fa.SetValue(t.Ext, t.ProgId);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public static void RemoveShortcuts()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

                string[] lnkNames = { "ZENQ OFFICE.lnk", "ZenQ Writer.lnk", "ZenQ Calc.lnk", "ZenQ Slides.lnk", "ZenQ PDF.lnk", "ZenQOffice.lnk", "ZenQ Office.lnk", "GenOffice.lnk" };
                foreach (string name in lnkNames)
                {
                    string dPath = Path.Combine(desktop, name);
                    if (File.Exists(dPath)) File.Delete(dPath);

                    string sPath = Path.Combine(startMenu, name);
                    if (File.Exists(sPath)) File.Delete(sPath);
                }
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
                        key.SetValue("DisplayVersion", "5.4");
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

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ZenQOffice.Core
{
    /// <summary>
    /// Quản lý đăng ký liên kết tệp (File Association) với Windows Registry
    /// Giúp Windows nhận dạng ZenQ Office (Writer, Calc, Slides, PDF) làm ứng dụng mặc định
    /// </summary>
    public static class FileAssociationManager
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OPENASINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pcszFile;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pcszClass;
            public uint oaifInFlags;
        }

        [DllImport("shell32.dll", EntryPoint = "SHOpenWithDialog", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SHOpenWithDialog(IntPtr hWndParent, ref OPENASINFO oainfo);

        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;
        private const uint OAIF_ALLOW_REGISTRATION = 0x00000001;
        private const uint OAIF_FORCE_REGISTRATION = 0x00000008;

        public struct FileTypeInfo
        {
            public string Extension;
            public string ProgId;
            public string Description;
            public string AppParam; // e.g. "-word", "-calc", "-slides", "-pdf"
            public string IconRelativePath;

            public FileTypeInfo(string ext, string progId, string desc, string param, string iconRel)
            {
                Extension = ext;
                ProgId = progId;
                Description = desc;
                AppParam = param;
                IconRelativePath = iconRel;
            }
        }

        public static readonly FileTypeInfo[] SupportedFileTypes = new FileTypeInfo[]
        {
            // Microsoft Word & Documents
            new FileTypeInfo(".docx", "ZenQOffice.Docx", "Tài liệu Microsoft Word (.docx)", "-word", "icon\\writer.ico"),
            new FileTypeInfo(".doc", "ZenQOffice.Doc", "Tài liệu Microsoft Word 97-2003 (.doc)", "-word", "icon\\writer.ico"),
            new FileTypeInfo(".rtf", "ZenQOffice.Rtf", "Tài liệu Rich Text Format (.rtf)", "-word", "icon\\writer.ico"),
            new FileTypeInfo(".txt", "ZenQOffice.Txt", "Tài liệu Văn bản thuần (.txt)", "-word", "icon\\writer.ico"),
            new FileTypeInfo(".dotx", "ZenQOffice.Dotx", "Mẫu văn bản Word (.dotx)", "-word", "icon\\writer.ico"),

            // Microsoft Excel & Spreadsheets
            new FileTypeInfo(".xlsx", "ZenQOffice.Xlsx", "Bảng tính Microsoft Excel (.xlsx)", "-calc", "icon\\calc.ico"),
            new FileTypeInfo(".xls", "ZenQOffice.Xls", "Bảng tính Microsoft Excel 97-2003 (.xls)", "-calc", "icon\\calc.ico"),
            new FileTypeInfo(".csv", "ZenQOffice.Csv", "Tệp dữ liệu phân cách (.csv)", "-calc", "icon\\calc.ico"),
            new FileTypeInfo(".tsv", "ZenQOffice.Tsv", "Tệp dữ liệu Tab-Delimited (.tsv)", "-calc", "icon\\calc.ico"),
            new FileTypeInfo(".ods", "ZenQOffice.Ods", "Bảng tính OpenDocument (.ods)", "-calc", "icon\\calc.ico"),

            // Microsoft PowerPoint & Presentations
            new FileTypeInfo(".pptx", "ZenQOffice.Pptx", "Bài trình chiếu Microsoft PowerPoint (.pptx)", "-slides", "icon\\present.ico"),
            new FileTypeInfo(".ppt", "ZenQOffice.Ppt", "Bài trình chiếu PowerPoint 97-2003 (.ppt)", "-slides", "icon\\present.ico"),
            new FileTypeInfo(".ppsx", "ZenQOffice.Ppsx", "Trình chiếu PowerPoint Show (.ppsx)", "-slides", "icon\\present.ico"),
            new FileTypeInfo(".potx", "ZenQOffice.Potx", "Mẫu trình chiếu PowerPoint (.potx)", "-slides", "icon\\present.ico"),

            // PDF Documents
            new FileTypeInfo(".pdf", "ZenQOffice.Pdf", "Tài liệu Adobe PDF (.pdf)", "-pdf", "icon\\pdf.ico")
        };

        /// <summary>
        /// Đăng ký toàn bộ định dạng tệp Office vào Windows Registry cho người dùng hiện tại (HKCU)
        /// Không cần quyền Administrator, hoạt động ngay lập tức 100% trên Windows 10/11!
        /// </summary>
        public static bool RegisterAllFileAssociations(string customExePath = null)
        {
            try
            {
                string exePath = !string.IsNullOrEmpty(customExePath) && File.Exists(customExePath)
                    ? customExePath
                    : Application.ExecutablePath;

                string baseDir = Path.GetDirectoryName(exePath);
                string mainIcoPath = Path.Combine(baseDir, "ZenQOffice.ico");
                if (!File.Exists(mainIcoPath)) mainIcoPath = exePath;

                // 1. Đăng ký các ProgID và cấu hình lệnh mở
                foreach (var ft in SupportedFileTypes)
                {
                    string iconPath = Path.Combine(baseDir, ft.IconRelativePath);
                    if (!File.Exists(iconPath)) iconPath = mainIcoPath;

                    string progKeyPath = $@"Software\Classes\{ft.ProgId}";
                    using (RegistryKey progKey = Registry.CurrentUser.CreateSubKey(progKeyPath))
                    {
                        if (progKey != null)
                        {
                            progKey.SetValue("", ft.Description);
                            progKey.SetValue("FriendlyTypeName", ft.Description);

                            using (RegistryKey iconKey = progKey.CreateSubKey("DefaultIcon"))
                            {
                                iconKey?.SetValue("", $"{iconPath},0");
                            }

                            using (RegistryKey cmdKey = progKey.CreateSubKey(@"shell\open\command"))
                            {
                                // Mở thẳng app nhánh độc lập với file được chọn:
                                cmdKey?.SetValue("", $"\"{exePath}\" {ft.AppParam} \"%1\"");
                            }

                            using (RegistryKey openKey = progKey.CreateSubKey(@"shell\open"))
                            {
                                openKey?.SetValue("FriendlyAppName", "ZenQ Office");
                            }
                        }
                    }

                    // 2. Liên kết phần mở rộng tệp với ProgID trong HKCU\Software\Classes
                    string extKeyPath = $@"Software\Classes\{ft.Extension}";
                    using (RegistryKey extKey = Registry.CurrentUser.CreateSubKey(extKeyPath))
                    {
                        if (extKey != null)
                        {
                            extKey.SetValue("", ft.ProgId);
                            if (ft.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                            {
                                extKey.SetValue("Content Type", "application/pdf");
                                extKey.SetValue("PerceivedType", "document");
                            }

                            using (RegistryKey openWith = extKey.CreateSubKey("OpenWithProgids"))
                            {
                                openWith?.SetValue(ft.ProgId, string.Empty);
                            }

                            using (RegistryKey owl = extKey.CreateSubKey(@"OpenWithList\ZenQOffice.exe")) { }
                        }
                    }

                    // 2b. SystemFileAssociations
                    try
                    {
                        using (RegistryKey sfa = Registry.CurrentUser.CreateSubKey($@"Software\Classes\SystemFileAssociations\{ft.Extension}\OpenWithList\ZenQOffice.exe")) { }
                    }
                    catch { }

                    // 2c. Explorer\FileExts OpenWithProgids & OpenWithList
                    try
                    {
                        using (RegistryKey feProg = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{ft.Extension}\OpenWithProgids"))
                        {
                            feProg?.SetValue(ft.ProgId, new byte[0], RegistryValueKind.Binary);
                        }
                        AddToOpenWithList(ft.Extension, "ZenQOffice.exe");
                    }
                    catch { }
                }

                // 3. Đăng ký ứng dụng với Windows (Applications\ZenQOffice.exe)
                string appKeyPath = @"Software\Classes\Applications\ZenQOffice.exe";
                using (RegistryKey appKey = Registry.CurrentUser.CreateSubKey(appKeyPath))
                {
                    if (appKey != null)
                    {
                        appKey.SetValue("FriendlyAppName", "ZenQ Office Suite");
                        appKey.SetValue("ApplicationCompany", "NextGenEduSpace Studio");
                        appKey.SetValue("ApplicationDescription", "Bộ ứng dụng văn phòng thế hệ mới ZenQ Office");
                        using (RegistryKey iconKey = appKey.CreateSubKey("DefaultIcon"))
                        {
                            iconKey?.SetValue("", $"{mainIcoPath},0");
                        }

                        using (RegistryKey cmdKey = appKey.CreateSubKey(@"shell\open\command"))
                        {
                            cmdKey?.SetValue("", $"\"{exePath}\" \"%1\"");
                        }

                        using (RegistryKey supTypes = appKey.CreateSubKey("SupportedTypes"))
                        {
                            if (supTypes != null)
                            {
                                foreach (var ft in SupportedFileTypes)
                                {
                                    supTypes.SetValue(ft.Extension, string.Empty);
                                }
                            }
                        }
                    }
                }

                // 4. Đăng ký Windows Registered Applications để xuất hiện chuẩn trong Settings -> Default Apps
                string regAppKeyPath = @"Software\RegisteredApplications";
                using (RegistryKey regKey = Registry.CurrentUser.CreateSubKey(regAppKeyPath))
                {
                    regKey?.SetValue("ZenQOffice", @"Software\ZenQOffice\Capabilities");
                }

                string capKeyPath = @"Software\ZenQOffice\Capabilities";
                using (RegistryKey capKey = Registry.CurrentUser.CreateSubKey(capKeyPath))
                {
                    if (capKey != null)
                    {
                        capKey.SetValue("ApplicationName", "ZenQ Office Suite");
                        capKey.SetValue("ApplicationDescription", "Bộ ứng dụng văn phòng thế hệ mới ZenQ Office (Writer, Calc, Slides, PDF)");

                        using (RegistryKey faKey = capKey.CreateSubKey("FileAssociations"))
                        {
                            if (faKey != null)
                            {
                                foreach (var ft in SupportedFileTypes)
                                {
                                    faKey.SetValue(ft.Extension, ft.ProgId);
                                }
                            }
                        }
                    }
                }

                // 5. Đăng ký App Paths trong Windows
                string appPathKey = @"Software\Microsoft\Windows\CurrentVersion\App Paths\ZenQOffice.exe";
                using (RegistryKey apKey = Registry.CurrentUser.CreateSubKey(appPathKey))
                {
                    if (apKey != null)
                    {
                        apKey.SetValue("", exePath);
                        apKey.SetValue("Path", baseDir);
                    }
                }

                // 6. Phát tín hiệu SHChangeNotify để Windows Explorer cập nhật ngay lập tức mà không cần khởi động lại máy
                try
                {
                    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                }
                catch { }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RegisterAllFileAssociations error: " + ex.Message);
                return false;
            }
        }

        private static void AddToOpenWithList(string extension, string exeName)
        {
            try
            {
                string owlPath = $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}\OpenWithList";
                using (RegistryKey owlKey = Registry.CurrentUser.CreateSubKey(owlPath))
                {
                    if (owlKey == null) return;
                    string foundLetter = null;
                    string[] valNames = owlKey.GetValueNames();
                    foreach (string vName in valNames)
                    {
                        if (vName.Length == 1 && char.IsLetter(vName[0]))
                        {
                            object vVal = owlKey.GetValue(vName);
                            if (vVal != null && string.Equals(vVal.ToString(), exeName, StringComparison.OrdinalIgnoreCase))
                            {
                                foundLetter = vName.ToLowerInvariant();
                                break;
                            }
                        }
                    }

                    if (foundLetter == null)
                    {
                        for (char c = 'a'; c <= 'z'; c++)
                        {
                            string candidate = c.ToString();
                            if (owlKey.GetValue(candidate) == null)
                            {
                                owlKey.SetValue(candidate, exeName);
                                foundLetter = candidate;
                                break;
                            }
                        }
                    }

                    if (foundLetter != null)
                    {
                        string mru = owlKey.GetValue("MRUList") as string ?? "";
                        string newMru = foundLetter + mru.Replace(foundLetter, "");
                        owlKey.SetValue("MRUList", newMru);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Kiểm tra xem ứng dụng cụ thể (-word, -calc, -slides, -pdf) hoặc tất cả có được đặt làm mặc định không
        /// Kiểm tra chính xác qua UserChoice (Windows 10/11) và HKCU\Software\Classes
        /// </summary>
        public static bool IsAppAssociated(string appParam)
        {
            try
            {
                if (string.IsNullOrEmpty(appParam) || appParam.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    return IsAppAssociated("-word") && IsAppAssociated("-calc") &&
                           IsAppAssociated("-slides") && IsAppAssociated("-pdf");
                }

                string primaryExt = ".docx";
                if (string.Equals(appParam, "-calc", StringComparison.OrdinalIgnoreCase)) primaryExt = ".xlsx";
                else if (string.Equals(appParam, "-slides", StringComparison.OrdinalIgnoreCase)) primaryExt = ".pptx";
                else if (string.Equals(appParam, "-pdf", StringComparison.OrdinalIgnoreCase)) primaryExt = ".pdf";

                // 1. Kiểm tra UserChoice trong Explorer\FileExts (Windows 10/11 luôn ưu tiên UserChoice trước)
                string userChoiceKey = $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{primaryExt}\UserChoice";
                using (RegistryKey ucKey = Registry.CurrentUser.OpenSubKey(userChoiceKey))
                {
                    if (ucKey != null)
                    {
                        object progIdVal = ucKey.GetValue("ProgId");
                        if (progIdVal != null)
                        {
                            string progIdStr = progIdVal.ToString();
                            return progIdStr.StartsWith("ZenQOffice.", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }

                // 2. Kiểm tra trong HKCU\Software\Classes
                string extKeyPath = $@"Software\Classes\{primaryExt}";
                using (RegistryKey extKey = Registry.CurrentUser.OpenSubKey(extKeyPath))
                {
                    if (extKey != null)
                    {
                        object val = extKey.GetValue("");
                        if (val != null && val.ToString().StartsWith("ZenQOffice.", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                // 3. Kiểm tra trong HKEY_CLASSES_ROOT (tổng hợp hệ thống)
                using (RegistryKey crKey = Registry.ClassesRoot.OpenSubKey(primaryExt))
                {
                    if (crKey != null)
                    {
                        object val = crKey.GetValue("");
                        if (val != null && val.ToString().StartsWith("ZenQOffice.", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Đăng ký liên kết tệp mặc định cho từng ứng dụng cụ thể (-word, -calc, -slides, -pdf)
        /// </summary>
        public static bool RegisterApp(string appParam, string customExePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(appParam) || appParam.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    return RegisterAllFileAssociations(customExePath);
                }

                string exePath = !string.IsNullOrEmpty(customExePath) && File.Exists(customExePath)
                    ? customExePath
                    : Application.ExecutablePath;

                string baseDir = Path.GetDirectoryName(exePath);
                string mainIcoPath = Path.Combine(baseDir, "ZenQOffice.ico");
                if (!File.Exists(mainIcoPath)) mainIcoPath = exePath;

                foreach (var ft in SupportedFileTypes)
                {
                    if (!string.Equals(ft.AppParam, appParam, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string iconPath = Path.Combine(baseDir, ft.IconRelativePath);
                    if (!File.Exists(iconPath)) iconPath = mainIcoPath;

                    string progKeyPath = $@"Software\Classes\{ft.ProgId}";
                    using (RegistryKey progKey = Registry.CurrentUser.CreateSubKey(progKeyPath))
                    {
                        if (progKey != null)
                        {
                            progKey.SetValue("", ft.Description);
                            progKey.SetValue("FriendlyTypeName", ft.Description);

                            using (RegistryKey iconKey = progKey.CreateSubKey("DefaultIcon"))
                            {
                                iconKey?.SetValue("", $"{iconPath},0");
                            }

                            using (RegistryKey cmdKey = progKey.CreateSubKey(@"shell\open\command"))
                            {
                                cmdKey?.SetValue("", $"\"{exePath}\" {ft.AppParam} \"%1\"");
                            }

                            using (RegistryKey openKey = progKey.CreateSubKey(@"shell\open"))
                            {
                                openKey?.SetValue("FriendlyAppName", "ZenQ Office");
                            }
                        }
                    }

                    string extKeyPath = $@"Software\Classes\{ft.Extension}";
                    using (RegistryKey extKey = Registry.CurrentUser.CreateSubKey(extKeyPath))
                    {
                        if (extKey != null)
                        {
                            extKey.SetValue("", ft.ProgId);
                            if (ft.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                            {
                                extKey.SetValue("Content Type", "application/pdf");
                                extKey.SetValue("PerceivedType", "document");
                            }

                            using (RegistryKey openWith = extKey.CreateSubKey("OpenWithProgids"))
                            {
                                openWith?.SetValue(ft.ProgId, string.Empty);
                            }

                            using (RegistryKey owl = extKey.CreateSubKey(@"OpenWithList\ZenQOffice.exe")) { }
                        }
                    }

                    try
                    {
                        using (RegistryKey sfa = Registry.CurrentUser.CreateSubKey($@"Software\Classes\SystemFileAssociations\{ft.Extension}\OpenWithList\ZenQOffice.exe")) { }
                    }
                    catch { }

                    try
                    {
                        using (RegistryKey feProg = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{ft.Extension}\OpenWithProgids"))
                        {
                            feProg?.SetValue(ft.ProgId, new byte[0], RegistryValueKind.Binary);
                        }
                        AddToOpenWithList(ft.Extension, "ZenQOffice.exe");
                    }
                    catch { }

                    string capFaKeyPath = @"Software\ZenQOffice\Capabilities\FileAssociations";
                    using (RegistryKey capKey = Registry.CurrentUser.CreateSubKey(capFaKeyPath))
                    {
                        capKey?.SetValue(ft.Extension, ft.ProgId);
                    }
                }

                using (RegistryKey regKey = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))
                {
                    regKey?.SetValue("ZenQOffice", @"Software\ZenQOffice\Capabilities");
                }

                try
                {
                    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                }
                catch { }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RegisterApp error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Gỡ bỏ liên kết tệp mặc định cho từng ứng dụng cụ thể (-word, -calc, -slides, -pdf)
        /// </summary>
        public static bool UnregisterApp(string appParam)
        {
            try
            {
                if (string.IsNullOrEmpty(appParam) || appParam.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    return UnregisterAllFileAssociations();
                }

                foreach (var ft in SupportedFileTypes)
                {
                    if (!string.Equals(ft.AppParam, appParam, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string extKeyPath = $@"Software\Classes\{ft.Extension}";
                    try
                    {
                        using (RegistryKey extKey = Registry.CurrentUser.OpenSubKey(extKeyPath, true))
                        {
                            if (extKey != null)
                            {
                                object val = extKey.GetValue("");
                                if (val != null && string.Equals(val.ToString(), ft.ProgId, StringComparison.OrdinalIgnoreCase))
                                {
                                    extKey.DeleteValue("", false);
                                }

                                try
                                {
                                    using (RegistryKey owKey = extKey.OpenSubKey("OpenWithProgids", true))
                                    {
                                        owKey?.DeleteValue(ft.ProgId, false);
                                    }
                                }
                                catch { }

                                if (extKey.ValueCount == 0 && extKey.SubKeyCount == 0)
                                {
                                    Registry.CurrentUser.DeleteSubKey(extKeyPath, false);
                                }
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ft.ProgId}", false);
                    }
                    catch { }

                    try
                    {
                        using (RegistryKey capKey = Registry.CurrentUser.OpenSubKey(@"Software\ZenQOffice\Capabilities\FileAssociations", true))
                        {
                            capKey?.DeleteValue(ft.Extension, false);
                        }
                    }
                    catch { }
                }

                try
                {
                    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                }
                catch { }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UnregisterApp error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Gỡ bỏ toàn bộ liên kết tệp mặc định của ZenQ Office trong Windows Registry
        /// </summary>
        public static bool UnregisterAllFileAssociations()
        {
            bool res1 = UnregisterApp("-word");
            bool res2 = UnregisterApp("-calc");
            bool res3 = UnregisterApp("-slides");
            bool res4 = UnregisterApp("-pdf");
            return res1 && res2 && res3 && res4;
        }

        /// <summary>
        /// Bật/Tắt liên kết tệp mặc định cho từng ứng dụng (hỗ trợ hiển thị hộp thoại xác nhận Windows)
        /// </summary>
        public static bool ToggleAppAssociation(string appParam, IntPtr parentHwnd = default)
        {
            if (IsAppAssociated(appParam))
            {
                return UnregisterApp(appParam);
            }
            else
            {
                bool res = RegisterApp(appParam);
                if (string.Equals(appParam, "-pdf", StringComparison.OrdinalIgnoreCase))
                {
                    // Windows 10/11 bảo vệ UserChoice cho PDF. Gọi hộp thoại chọn ứng dụng chính thức:
                    PromptWindowsDefaultApp(".pdf", parentHwnd);
                    if (!IsAppAssociated("-pdf"))
                    {
                        OpenWindowsDefaultAppsSettings("ZenQOffice");
                    }
                }
                return res;
            }
        }

        /// <summary>
        /// Hiển thị hộp thoại chính thức của Windows để người dùng xác nhận đặt ứng dụng làm mặc định
        /// </summary>
        public static bool PromptWindowsDefaultApp(string extension = ".pdf", IntPtr parentHwnd = default)
        {
            try
            {
                string tempDir = Path.GetTempPath();
                string tempFile = Path.Combine(tempDir, "ZenQOffice_default" + extension);
                if (!File.Exists(tempFile))
                {
                    if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        byte[] minimalPdf = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Count 0/Kids[]>>endobj\nxref\n0 3\n0000000000 65535 f\n0000000009 00000 n\n0000000052 00000 n\ntrailer<</Size 3/Root 1 0 R>>\nstartxref\n102\n%%EOF");
                        File.WriteAllBytes(tempFile, minimalPdf);
                    }
                    else
                    {
                        File.WriteAllText(tempFile, "ZenQ Office Association Test");
                    }
                }

                OPENASINFO info = new OPENASINFO();
                info.pcszFile = tempFile;
                info.pcszClass = null;
                info.oaifInFlags = OAIF_ALLOW_REGISTRATION | OAIF_FORCE_REGISTRATION;
                int res = SHOpenWithDialog(parentHwnd, ref info);
                return res == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PromptWindowsDefaultApp error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra xem ZenQ Office đã được liên kết với các định dạng chính (.docx, .xlsx, .pptx) hay chưa
        /// </summary>
        public static bool IsAssociated(string extension = ".docx")
        {
            try
            {
                string extKeyPath = $@"Software\Classes\{extension}";
                using (RegistryKey extKey = Registry.CurrentUser.OpenSubKey(extKeyPath))
                {
                    if (extKey != null)
                    {
                        object val = extKey.GetValue("");
                        if (val != null && val.ToString().StartsWith("ZenQOffice.", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Mở trang Cài đặt Ứng dụng mặc định của Windows 10/11 (chỉ định thẳng tới ZenQ Office)
        /// </summary>
        public static void OpenWindowsDefaultAppsSettings(string appRegistryName = "ZenQOffice")
        {
            try
            {
                // Trên Windows 11: Mở thẳng trang thiết lập của ZenQ Office trong Windows Settings
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"ms-settings:defaultapps?registeredAppUser={appRegistryName}",
                    UseShellExecute = true
                });
            }
            catch
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "ms-settings:defaultapps",
                        UseShellExecute = true
                    });
                }
                catch
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "control.exe",
                            Arguments = "/name Microsoft.DefaultPrograms",
                            UseShellExecute = true
                        });
                    }
                    catch { }
                }
            }
        }
    }
}

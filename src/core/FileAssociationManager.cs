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

        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

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
                            using (RegistryKey openWith = extKey.CreateSubKey("OpenWithProgids"))
                            {
                                openWith?.SetValue(ft.ProgId, string.Empty);
                            }
                        }
                    }
                }

                // 3. Đăng ký ứng dụng với Windows (Applications\ZenQOffice.exe)
                string appKeyPath = @"Software\Classes\Applications\ZenQOffice.exe";
                using (RegistryKey appKey = Registry.CurrentUser.CreateSubKey(appKeyPath))
                {
                    if (appKey != null)
                    {
                        appKey.SetValue("FriendlyAppName", "ZenQ Office Suite");
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
        /// Mở trang Cài đặt Ứng dụng mặc định của Windows 10/11
        /// </summary>
        public static void OpenWindowsDefaultAppsSettings()
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

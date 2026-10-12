using System;
using System.IO;
using System.Windows.Forms;

namespace ZenQOffice.Core
{
    public static class DesktopShortcutManager
    {
        public static string GetDesktopPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        public static string GetShortcutPath(string name)
        {
            return Path.Combine(GetDesktopPath(), name + ".lnk");
        }

        public static bool IsShortcutExists(string name)
        {
            try
            {
                return File.Exists(GetShortcutPath(name));
            }
            catch
            {
                return false;
            }
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLinkClass {}

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        [System.Runtime.InteropServices.Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cchMaxName);
            void SetDescription([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out ushort pwHotkey);
            void SetHotkey(ushort wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszFile);
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        [System.Runtime.InteropServices.Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
        private interface IPropertyStore
        {
            uint GetCount();
            void GetAt(uint iProp, out PropertyKey pkey);
            void GetValue(ref PropertyKey key, out PropVariant pv);
            void SetValue(ref PropertyKey key, ref PropVariant pv);
            void Commit();
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 4)]
        private struct PropertyKey
        {
            public Guid fmtid;
            public uint pid;
            public PropertyKey(Guid guid, uint id) { fmtid = guid; pid = id; }
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
        private struct PropVariant
        {
            [System.Runtime.InteropServices.FieldOffset(0)] public ushort vt;
            [System.Runtime.InteropServices.FieldOffset(8)] public IntPtr pwszVal;

            public static PropVariant FromString(string val)
            {
                PropVariant pv = new PropVariant();
                pv.vt = 31; // VT_LPWSTR
                pv.pwszVal = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUni(val);
                return pv;
            }

            public void Clear()
            {
                PropVariantClear(ref this);
            }

            [System.Runtime.InteropServices.DllImport("ole32.dll")]
            private static extern int PropVariantClear(ref PropVariant pvar);
        }

        public static void SetShortcutAppId(string lnkPath, string appId)
        {
            if (string.IsNullOrEmpty(lnkPath) || !File.Exists(lnkPath) || string.IsNullOrEmpty(appId)) return;
            try
            {
                IShellLinkW link = (IShellLinkW)new ShellLinkClass();
                ((System.Runtime.InteropServices.ComTypes.IPersistFile)link).Load(lnkPath, 2); // STGM_READWRITE = 2

                IPropertyStore store = (IPropertyStore)link;
                PropertyKey key = new PropertyKey(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5); // PKEY_AppUserModel_ID
                PropVariant pv = PropVariant.FromString(appId);
                store.SetValue(ref key, ref pv);
                store.Commit();
                pv.Clear();

                ((System.Runtime.InteropServices.ComTypes.IPersistFile)link).Save(lnkPath, true);
            }
            catch { }
        }

        public static bool CreateShortcut(string name, string args, string iconRelativePath, string description)
        {
            try
            {
                string exePath = Application.ExecutablePath;
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string lnkPath = GetShortcutPath(name);
                string iconPath = Path.Combine(baseDir, iconRelativePath);

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return false;

                dynamic shell = Activator.CreateInstance(shellType);
                var sc = shell.CreateShortcut(lnkPath);
                sc.TargetPath = exePath;
                sc.Arguments = args;
                sc.WorkingDirectory = baseDir;
                if (File.Exists(iconPath))
                {
                    sc.IconLocation = iconPath + ",0";
                }
                sc.Description = description;
                sc.Save();

                // Gán AppUserModelID riêng biệt cho từng Shortcut để Windows Taskbar không nhầm lẫn
                string appId = "ZenQOffice.Suite.Hub";
                if (!string.IsNullOrEmpty(args))
                {
                    string a = args.ToLowerInvariant();
                    if (a.Contains("-writer") || a.Contains("-word") || a.Contains("-doc")) appId = "ZenQOffice.Suite.Writer";
                    else if (a.Contains("-calc") || a.Contains("-excel") || a.Contains("-sheet")) appId = "ZenQOffice.Suite.Calc";
                    else if (a.Contains("-slides") || a.Contains("-present") || a.Contains("-ppt")) appId = "ZenQOffice.Suite.Slides";
                    else if (a.Contains("-pdf")) appId = "ZenQOffice.Suite.Pdf";
                }
                SetShortcutAppId(lnkPath, appId);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool DeleteShortcut(string name)
        {
            try
            {
                string lnkPath = GetShortcutPath(name);
                if (File.Exists(lnkPath))
                {
                    File.Delete(lnkPath);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool ToggleShortcut(string name, string args, string iconRelativePath, string description)
        {
            if (IsShortcutExists(name))
            {
                return DeleteShortcut(name);
            }
            else
            {
                return CreateShortcut(name, args, iconRelativePath, description);
            }
        }

        public static void CreateMainHubShortcut()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string ico = Path.Combine(baseDir, "ZenQOffice.ico");
            CreateShortcut("ZenQ Office", "", File.Exists(ico) ? "ZenQOffice.ico" : "ZenQOffice.ico", "ZenQ Office - Bộ ứng dụng văn phòng tích hợp đa năng");
        }

        public static void CreateAllShortcuts()
        {
            CreateMainHubShortcut();
            CreateAllBranchShortcuts();
        }

        public static void CreateAllBranchShortcuts()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string writerIco = Path.Combine(baseDir, "icon", "writer.ico");
            string calcIco = Path.Combine(baseDir, "icon", "calc.ico");
            string slidesIco = Path.Combine(baseDir, "icon", "present.ico");
            string pdfIco = Path.Combine(baseDir, "icon", "pdf.ico");

            CreateShortcut("ZenQ Writer", "-word", File.Exists(writerIco) ? "icon\\writer.ico" : "ZenQOffice.ico", "ZenQ Writer - Soạn thảo văn bản độc lập");
            CreateShortcut("ZenQ Calc", "-calc", File.Exists(calcIco) ? "icon\\calc.ico" : "ZenQOffice.ico", "ZenQ Calc - Bảng tính điện tử độc lập");
            CreateShortcut("ZenQ Slides", "-slides", File.Exists(slidesIco) ? "icon\\present.ico" : "ZenQOffice.ico", "ZenQ Slides - Thiết kế trình chiếu độc lập");
            CreateShortcut("ZenQ PDF", "-pdf", File.Exists(pdfIco) ? "icon\\pdf.ico" : "ZenQOffice.ico", "ZenQ PDF - Đọc & biên tập tài liệu PDF");
        }

        public static void DeleteAllBranchShortcuts()
        {
            DeleteShortcut("ZenQ Writer");
            DeleteShortcut("ZenQ Calc");
            DeleteShortcut("ZenQ Slides");
            DeleteShortcut("ZenQ PDF");
        }
    }
}

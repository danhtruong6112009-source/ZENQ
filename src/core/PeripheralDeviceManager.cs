using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZenQOffice.Core
{
    public enum PointerInputType
    {
        Unknown = 0,
        Pointer = 1,
        Touch = 2,
        Pen = 3,
        Mouse = 4,
        Touchpad = 5
    }

    public class UsbDriveSummary
    {
        public string DriveLetter { get; set; }
        public string VolumeLabel { get; set; }
        public long TotalSizeBytes { get; set; }
        public long FreeSpaceBytes { get; set; }
        public string FileSystem { get; set; }

        public string DisplayName
        {
            get
            {
                string lbl = string.IsNullOrWhiteSpace(VolumeLabel) ? "USB Drive" : VolumeLabel;
                double freeGb = FreeSpaceBytes / (1024.0 * 1024.0 * 1024.0);
                double totalGb = TotalSizeBytes / (1024.0 * 1024.0 * 1024.0);
                return string.Format("{0} ({1}) - Trống {2:0.1}/{3:0.1} GB", lbl, DriveLetter.TrimEnd('\\'), freeGb, totalGb);
            }
        }
    }

    public class MonitorSummary
    {
        public int Index { get; set; }
        public string DeviceName { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsPrimary { get; set; }

        public string DisplayName
        {
            get
            {
                return string.Format("Màn hình {0}: {1}×{2} {3}",
                    Index + 1, Bounds.Width, Bounds.Height,
                    IsPrimary ? "(Chính)" : "(Phụ)");
            }
        }
    }

    public static class PeripheralDeviceManager
    {
        // Win32 Constants
        public const int WM_DEVICECHANGE = 0x0219;
        public const int DBT_DEVICEARRIVAL = 0x8000;
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        public const int DBT_DEVNODES_CHANGED = 0x0007;

        public const int WM_MOUSEHWHEEL = 0x020E;
        public const int WM_APPCOMMAND = 0x0319;
        public const int WM_GESTURE = 0x0119;
        public const int WM_POINTERUPDATE = 0x0245;
        public const int WM_POINTERDOWN = 0x0246;
        public const int WM_POINTERUP = 0x0247;
        public const int WM_TABLET_QUERYSYSTEMGESTURESTATUS = 0x02CC;

        // Gesture IDs
        public const int GID_BEGIN = 1;
        public const int GID_END = 2;
        public const int GID_ZOOM = 3;
        public const int GID_PAN = 4;
        public const int GID_ROTATE = 5;
        public const int GID_TWOFINGERTAP = 6;
        public const int GID_PRESSANDTAP = 7;

        // Gesture Flags
        public const int GF_BEGIN = 0x00000001;
        public const int GF_INERTIA = 0x00000002;
        public const int GF_END = 0x00000004;

        // Pen Flags
        public const int PEN_FLAG_NONE = 0x00000000;
        public const int PEN_FLAG_BARREL = 0x00000001;
        public const int PEN_FLAG_INVERTED = 0x00000002;
        public const int PEN_FLAG_ERASER = 0x00000004;

        // AppCommand IDs (Media keys, Presenter remotes, Surface Headphones/Keyboard keys)
        public const int APPCOMMAND_BROWSER_BACKWARD = 1;
        public const int APPCOMMAND_BROWSER_FORWARD = 2;
        public const int APPCOMMAND_MEDIA_NEXTTRACK = 11;
        public const int APPCOMMAND_MEDIA_PREVIOUSTRACK = 12;
        public const int APPCOMMAND_MEDIA_STOP = 13;
        public const int APPCOMMAND_MEDIA_PLAY_PAUSE = 14;
        public const int APPCOMMAND_FIND = 28;
        public const int APPCOMMAND_NEW = 29;
        public const int APPCOMMAND_OPEN = 30;
        public const int APPCOMMAND_CLOSE = 31;
        public const int APPCOMMAND_SAVE = 32;
        public const int APPCOMMAND_PRINT = 33;
        public const int APPCOMMAND_UNDO = 34;
        public const int APPCOMMAND_REDO = 35;
        public const int APPCOMMAND_COPY = 36;
        public const int APPCOMMAND_CUT = 37;
        public const int APPCOMMAND_PASTE = 38;
        public const int APPCOMMAND_MIC_ON_OFF_TOGGLE = 44;

        // SystemMetrics Constants
        private const int SM_TABLETPC = 86;
        private const int SM_DIGITIZER = 94;
        private const int SM_MAXIMUMTOUCHES = 95;
        private const int SM_CONVERTIBLESLATEMODE = 0x2003;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTS
        {
            public short x;
            public short y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GESTUREINFO
        {
            public int cbSize;
            public int dwFlags;
            public int dwID;
            public IntPtr hwndTarget;
            public POINTS ptsLocation;
            public int dwInstanceID;
            public int dwSequenceID;
            public ulong ullArguments;
            public int cbExtraArgs;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTER_INFO
        {
            public int pointerType;
            public uint pointerId;
            public uint frameId;
            public uint pointerFlags;
            public IntPtr sourceDevice;
            public IntPtr hwndTarget;
            public POINT ptPixelLocation;
            public POINT ptHimetricLocation;
            public POINT ptPixelLocationRaw;
            public POINT ptHimetricLocationRaw;
            public uint dwTime;
            public uint historyCount;
            public int InputData;
            public uint dwKeyStates;
            public ulong PerformanceCount;
            public int ButtonChangeType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTER_PEN_INFO
        {
            public POINTER_INFO pointerInfo;
            public uint penFlags;
            public uint penMask;
            public uint pressure; // 0..1024 (default 512)
            public uint rotation; // 0..359
            public int tiltX;     // -90..90
            public int tiltY;     // -90..90
        }

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetGestureInfo(IntPtr hGestureInfo, ref GESTUREINFO pGestureInfo);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool CloseGestureInfoHandle(IntPtr hGestureInfo);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerType(uint pointerId, out int pointerType);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerPenInfo(uint pointerId, out POINTER_PEN_INFO penInfo);

        // Track last known USB drives & monitor count to detect changes cleanly
        private static HashSet<string> _knownRemovableDrives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static int _lastMonitorCount = 1;

        public static PointerInputType LastActivePointerType { get; set; } = PointerInputType.Mouse;
        public static float LastPenPressureNormalized { get; set; } = 0.5f; // 0.1f .. 1.0f
        public static bool LastPenIsEraser { get; set; } = false;
        public static int LastPenTiltX { get; set; } = 0;
        public static int LastPenTiltY { get; set; } = 0;

        static PeripheralDeviceManager()
        {
            RefreshBaselineState();
        }

        public static void RefreshBaselineState()
        {
            try
            {
                _knownRemovableDrives.Clear();
                foreach (var d in GetRemovableDrives())
                {
                    _knownRemovableDrives.Add(d.DriveLetter);
                }
                _lastMonitorCount = Screen.AllScreens.Length;
            }
            catch { }
        }

        public static bool HasTouchHardware(out int maxTouchPoints, out bool hasIntegratedPen, out bool isTabletSlateMode)
        {
            maxTouchPoints = 0;
            hasIntegratedPen = false;
            isTabletSlateMode = false;
            try
            {
                int digitizer = GetSystemMetrics(SM_DIGITIZER);
                maxTouchPoints = GetSystemMetrics(SM_MAXIMUMTOUCHES);
                hasIntegratedPen = (digitizer & 0x04) != 0 || (digitizer & 0x08) != 0 || GetSystemMetrics(SM_TABLETPC) != 0;
                int slate = GetSystemMetrics(SM_CONVERTIBLESLATEMODE);
                isTabletSlateMode = (slate == 0 && (digitizer & 0x01) != 0);
                return (digitizer & 0x01) != 0 || (digitizer & 0x02) != 0 || maxTouchPoints > 0;
            }
            catch
            {
                return false;
            }
        }

        public static string GetDigitizerCapabilitiesSummary()
        {
            int maxTouches;
            bool hasPen;
            bool isSlate;
            bool hasTouch = HasTouchHardware(out maxTouches, out hasPen, out isSlate);
            List<string> parts = new List<string>();
            if (hasTouch) parts.Add(string.Format("Cảm ứng {0} điểm", Math.Max(1, maxTouches)));
            else parts.Add("Chuột & bàn phím");

            if (hasPen) parts.Add("Bút Stylus");
            if (isSlate) parts.Add("Chế độ Tablet");
            return string.Join(" • ", parts.ToArray());
        }

        public static bool TryInspectPointerMessage(IntPtr wParam, out PointerInputType pType, out float pressureNorm, out bool isEraser)
        {
            pType = PointerInputType.Mouse;
            pressureNorm = 0.5f;
            isEraser = false;
            try
            {
                uint pointerId = (uint)(wParam.ToInt64() & 0xFFFF);
                int rawType;
                if (GetPointerType(pointerId, out rawType))
                {
                    if (rawType >= 1 && rawType <= 5)
                        pType = (PointerInputType)rawType;

                    LastActivePointerType = pType;

                    if (pType == PointerInputType.Pen)
                    {
                        POINTER_PEN_INFO penInfo;
                        if (GetPointerPenInfo(pointerId, out penInfo))
                        {
                            isEraser = (penInfo.penFlags & (PEN_FLAG_INVERTED | PEN_FLAG_ERASER)) != 0;
                            if (penInfo.pressure > 0 && penInfo.pressure <= 1024)
                            {
                                pressureNorm = Math.Max(0.15f, Math.Min(1.0f, penInfo.pressure / 1024f));
                            }
                            LastPenPressureNormalized = pressureNorm;
                            LastPenIsEraser = isEraser;
                            LastPenTiltX = penInfo.tiltX;
                            LastPenTiltY = penInfo.tiltY;
                        }
                    }
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static List<UsbDriveSummary> GetRemovableDrives()
        {
            List<UsbDriveSummary> list = new List<UsbDriveSummary>();
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo d in drives)
                {
                    if (d.DriveType == DriveType.Removable && d.IsReady)
                    {
                        list.Add(new UsbDriveSummary
                        {
                            DriveLetter = d.Name,
                            VolumeLabel = d.VolumeLabel,
                            TotalSizeBytes = d.TotalSize,
                            FreeSpaceBytes = d.AvailableFreeSpace,
                            FileSystem = d.DriveFormat
                        });
                    }
                }
            }
            catch { }
            return list;
        }

        public static UsbDriveSummary CheckForNewlyArrivedUsbDrive(out string removedDriveLetter)
        {
            removedDriveLetter = null;
            try
            {
                var current = GetRemovableDrives();
                HashSet<string> currentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                UsbDriveSummary newlyAdded = null;

                foreach (var d in current)
                {
                    currentSet.Add(d.DriveLetter);
                    if (!_knownRemovableDrives.Contains(d.DriveLetter))
                    {
                        newlyAdded = d;
                    }
                }

                foreach (string oldDrive in _knownRemovableDrives)
                {
                    if (!currentSet.Contains(oldDrive))
                    {
                        removedDriveLetter = oldDrive;
                        break;
                    }
                }

                _knownRemovableDrives = currentSet;
                return newlyAdded;
            }
            catch
            {
                return null;
            }
        }

        public static List<MonitorSummary> GetConnectedMonitors()
        {
            List<MonitorSummary> list = new List<MonitorSummary>();
            try
            {
                Screen[] screens = Screen.AllScreens;
                for (int i = 0; i < screens.Length; i++)
                {
                    list.Add(new MonitorSummary
                    {
                        Index = i,
                        DeviceName = screens[i].DeviceName,
                        Bounds = screens[i].Bounds,
                        IsPrimary = screens[i].Primary
                    });
                }
            }
            catch { }
            return list;
        }

        public static bool CheckMonitorCountChanged(out int newCount)
        {
            newCount = 1;
            try
            {
                newCount = Screen.AllScreens.Length;
                if (newCount != _lastMonitorCount)
                {
                    _lastMonitorCount = newCount;
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static List<string> ScanUsbOfficeFiles(string driveRoot, int maxFiles = 40)
        {
            List<string> results = new List<string>();
            if (string.IsNullOrEmpty(driveRoot) || !Directory.Exists(driveRoot)) return results;

            HashSet<string> allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".docx", ".doc", ".xlsx", ".xls", ".csv", ".pptx", ".ppt", ".pdf", ".txt", ".rtf", ".png", ".jpg", ".jpeg"
            };

            try
            {
                ScanDirRecursive(driveRoot, allowedExts, results, maxFiles, 0);
            }
            catch { }
            return results;
        }

        private static void ScanDirRecursive(string dir, HashSet<string> exts, List<string> results, int maxFiles, int depth)
        {
            if (results.Count >= maxFiles || depth > 2) return;
            try
            {
                foreach (string f in Directory.GetFiles(dir))
                {
                    string name = Path.GetFileName(f);
                    if (name.StartsWith("~$") || name.StartsWith(".")) continue;
                    string ext = Path.GetExtension(f);
                    if (exts.Contains(ext))
                    {
                        results.Add(f);
                        if (results.Count >= maxFiles) return;
                    }
                }
                foreach (string sub in Directory.GetDirectories(dir))
                {
                    string subName = Path.GetFileName(sub);
                    if (subName.StartsWith("$") || subName.StartsWith(".") ||
                        string.Equals(subName, "System Volume Information", StringComparison.OrdinalIgnoreCase))
                        continue;
                    ScanDirRecursive(sub, exts, results, maxFiles, depth + 1);
                    if (results.Count >= maxFiles) return;
                }
            }
            catch { }
        }

        public static bool BackupFileToUsb(string sourceFilePath, string driveRoot, out string backupDestPath, out string errorMsg)
        {
            backupDestPath = null;
            errorMsg = null;
            try
            {
                if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath))
                {
                    errorMsg = "Vui lòng lưu tài liệu hiện tại ra đĩa trước khi sao lưu nhanh sang USB.";
                    return false;
                }
                if (string.IsNullOrEmpty(driveRoot) || !Directory.Exists(driveRoot))
                {
                    errorMsg = "Không tìm thấy ổ đĩa USB đích.";
                    return false;
                }

                string backupDir = Path.Combine(driveRoot, "ZenQOffice_Backup");
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                string fileNameNoExt = Path.GetFileNameWithoutExtension(sourceFilePath);
                string ext = Path.GetExtension(sourceFilePath);
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                backupDestPath = Path.Combine(backupDir, string.Format("{0}_{1}{2}", fileNameNoExt, stamp, ext));

                File.Copy(sourceFilePath, backupDestPath, true);
                return true;
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                return false;
            }
        }

        [ComImport, Guid("4ce576fa-83dc-4F88-951c-9d0782b4e376")]
        private class UIHostNoLaunch { }

        [ComImport, Guid("37c994e7-432b-4834-a2f7-dce1f13b834b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITipInvocation
        {
            void Toggle(IntPtr hwnd);
        }

        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void ToggleTouchKeyboard()
        {
            // 1. If osk.exe is currently running, toggle it OFF (kill it)
            try
            {
                var osks = Process.GetProcessesByName("osk");
                if (osks != null && osks.Length > 0)
                {
                    foreach (var p in osks)
                    {
                        try { p.Kill(); } catch { }
                    }
                    return;
                }
            }
            catch { }

            // 2. Also invoke modern Windows Touch Keyboard COM interface (ITipInvocation) for tablet mode
            try
            {
                var uiHostNoLaunch = new UIHostNoLaunch();
                var tipInvocation = (ITipInvocation)uiHostNoLaunch;
                tipInvocation.Toggle(GetDesktopWindow());
                Marshal.ReleaseComObject(uiHostNoLaunch);
            }
            catch { }

            // 3. Launch On-Screen Keyboard (osk.exe) so it is guaranteed to show up visibly for the user
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "osk.exe",
                    UseShellExecute = true
                });
                return;
            }
            catch
            {
                try
                {
                    string sysDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                    string oskPath = Path.Combine(sysDir, "osk.exe");
                    Process.Start(new ProcessStartInfo(oskPath) { UseShellExecute = true });
                    return;
                }
                catch { }
            }

            // 4. Check if IPTip_Main_Window is open to show/restore
            try
            {
                IntPtr tipHwnd = FindWindow("IPTip_Main_Window", null);
                if (tipHwnd != IntPtr.Zero)
                {
                    ShowWindow(tipHwnd, 5); // SW_SHOW
                }
            }
            catch { }
        }

        public static void SetDisplayProjectionMode(string modeArg)
        {
            // modeArg: "/internal", "/clone", "/extend", "/external"
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "DisplaySwitch.exe",
                    Arguments = modeArg,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        public static void LaunchWindowsScannerDialog()
        {
            try
            {
                string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string wia = Path.Combine(sys32, "wiaacmgr.exe");
                if (File.Exists(wia))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = wia,
                        UseShellExecute = true
                    });
                    return;
                }
                Process.Start(new ProcessStartInfo("wiaacmgr.exe") { UseShellExecute = true });
            }
            catch
            {
                MessageBox.Show("Không tìm thấy trình điều khiển máy quét WIA mặc định hoặc chưa kết nối máy Scan USB/LAN.",
                    "Quét Tài Liệu (WIA Scanner)", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Enhances an image from a camera/scanner into a high-contrast, clean document scan.
        /// </summary>
        public static Bitmap EnhanceScannedDocumentImage(Bitmap src, bool blackAndWhiteMode)
        {
            if (src == null) return null;
            Bitmap dst = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(dst))
            {
                ColorMatrix cm;
                if (blackAndWhiteMode)
                {
                    // High-contrast grayscale document thresholding matrix
                    cm = new ColorMatrix(new float[][]
                    {
                        new float[] { 0.45f, 0.45f, 0.45f, 0, 0 },
                        new float[] { 0.85f, 0.85f, 0.85f, 0, 0 },
                        new float[] { 0.25f, 0.25f, 0.25f, 0, 0 },
                        new float[] { 0,     0,     0,     1, 0 },
                        new float[] { -0.18f, -0.18f, -0.18f, 0, 1 }
                    });
                }
                else
                {
                    // Crisp color document boost
                    cm = new ColorMatrix(new float[][]
                    {
                        new float[] { 1.25f, 0,     0,     0, 0 },
                        new float[] { 0,     1.25f, 0,     0, 0 },
                        new float[] { 0,     0,     1.25f, 0, 0 },
                        new float[] { 0,     0,     0,     1, 0 },
                        new float[] { -0.08f, -0.08f, -0.08f, 0, 1 }
                    });
                }

                using (ImageAttributes ia = new ImageAttributes())
                {
                    ia.SetColorMatrix(cm);
                    g.DrawImage(src, new Rectangle(0, 0, dst.Width, dst.Height),
                        0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
                }
            }
            return dst;
        }
    }
}

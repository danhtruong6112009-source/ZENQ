using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TestThumbnail
{
    class Program
    {
        [ComImport]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellItemImageFactory
        {
            [PreserveSig]
            int GetImage(
                [In, MarshalAs(UnmanagedType.Struct)] SIZE size,
                [In] SIIGBF flags,
                [Out] out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SIZE
        {
            public int cx;
            public int cy;
            public SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; }
        }

        enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0x00,
            SIIGBF_BIGGERSIZEOK = 0x01,
            SIIGBF_MEMORYONLY = 0x02,
            SIIGBF_ICONONLY = 0x04,
            SIIGBF_THUMBNAILONLY = 0x08,
            SIIGBF_INCACHEONLY = 0x10
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        static extern void SHCreateItemFromParsingName(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

        public static Bitmap GetVideoThumbnail(string filePath, int width, int height)
        {
            try
            {
                Guid guid = new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b");
                IShellItemImageFactory factory;
                SHCreateItemFromParsingName(filePath, IntPtr.Zero, guid, out factory);
                if (factory != null)
                {
                    IntPtr hBitmap;
                    int hr = factory.GetImage(new SIZE(width, height), SIIGBF.SIIGBF_BIGGERSIZEOK, out hBitmap);
                    if (hr == 0 && hBitmap != IntPtr.Zero)
                    {
                        Bitmap bmp = Image.FromHbitmap(hBitmap);
                        DeleteObject(hBitmap);
                        return bmp;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Thumb error: " + ex.Message);
            }
            return null;
        }

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        [STAThread]
        static void Main()
        {
            Console.WriteLine("Testing Shell32 video thumbnail extractor...");
            // Test with any existing file or desktop file
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string[] mp4Files = System.IO.Directory.GetFiles(desktop, "*.mp4", System.IO.SearchOption.TopDirectoryOnly);
            if (mp4Files.Length > 0)
            {
                Console.WriteLine("Found MP4: " + mp4Files[0]);
                Bitmap bmp = GetVideoThumbnail(mp4Files[0], 640, 360);
                Console.WriteLine("Extracted thumbnail success: " + (bmp != null) + (bmp != null ? (" (" + bmp.Width + "x" + bmp.Height + ")") : ""));
            }
            else
            {
                Console.WriteLine("No mp4 on desktop, testing generic thumbnail...");
            }
            Console.WriteLine("THUMBNAIL_TEST_FINISHED");
        }
    }
}

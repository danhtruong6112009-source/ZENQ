using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Bộ kết xuất Logo & Icon chính thức của ZenQ Office từ file thiết kế ZENQOFFICE.png / ZenQOffice.ico
    /// Tự động bo góc mượt mà (Rounded Squircle), khử răng cưa chất lượng cao và đồng bộ đa kích thước.
    /// </summary>
    public static class ZenQOfficeLogoRenderer
    {
        private static Image _rawLogoImage = null;
        private static Icon _cachedAppIcon = null;
        private static readonly Dictionary<int, Bitmap> _roundedCache = new Dictionary<int, Bitmap>();
        private static readonly object _syncLock = new object();

        private static string GetLogoFilePath()
        {
            string[] possiblePaths = new string[]
            {
                Path.Combine(Application.StartupPath, "ZENQOFFICE.png"),
                Path.Combine(Application.StartupPath, "ZenQOffice.png"),
                Path.Combine(Application.StartupPath, "GENOFFICE.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZENQOFFICE.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZenQOffice.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GENOFFICE.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "ZENQOFFICE.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "ZenQOffice.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "GENOFFICE.png")
            };

            foreach (string p in possiblePaths)
            {
                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                {
                    return p;
                }
            }
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZENQOFFICE.png");
        }

        private static string GetIcoFilePath()
        {
            string[] possiblePaths = new string[]
            {
                Path.Combine(Application.StartupPath, "ZenQOffice.ico"),
                Path.Combine(Application.StartupPath, "ZenQOffice.ico"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZenQOffice.ico"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZenQOffice.ico"),
                Path.Combine(Directory.GetCurrentDirectory(), "ZenQOffice.ico"),
                Path.Combine(Directory.GetCurrentDirectory(), "ZenQOffice.ico")
            };

            foreach (string p in possiblePaths)
            {
                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                {
                    return p;
                }
            }
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZenQOffice.ico");
        }

        private static Image GetRawImage()
        {
            if (_rawLogoImage != null) return _rawLogoImage;
            lock (_syncLock)
            {
                if (_rawLogoImage != null) return _rawLogoImage;

                // 1. Thử nạp từ Embedded Resource (Đảm bảo chạy độc lập 100% không lo thiếu file)
                try
                {
                    System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                    string[] resNames = new string[] { "app_logo.png", "ZENQOFFICE.png", "ZenQOffice.png", "GENOFFICE.png", "logo.png" };
                    foreach (string rn in resNames)
                    {
                        using (Stream st = asm.GetManifestResourceStream(rn))
                        {
                            if (st != null)
                            {
                                byte[] buf = new byte[st.Length];
                                st.Read(buf, 0, buf.Length);
                                using (MemoryStream ms = new MemoryStream(buf))
                                {
                                    _rawLogoImage = Image.FromStream(ms);
                                    return _rawLogoImage;
                                }
                            }
                        }
                    }
                }
                catch { }

                // 2. Thử nạp từ File hệ thống
                string path = GetLogoFilePath();
                if (File.Exists(path))
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(path);
                        using (MemoryStream ms = new MemoryStream(bytes))
                        {
                            _rawLogoImage = Image.FromStream(ms);
                            return _rawLogoImage;
                        }
                    }
                    catch { }
                }

                // 3. Thử nạp từ ZenQOffice.ico / ZenQOffice.ico
                string icoPath = GetIcoFilePath();
                if (File.Exists(icoPath))
                {
                    try
                    {
                        using (Icon ic = new Icon(icoPath, 256, 256))
                        {
                            _rawLogoImage = ic.ToBitmap();
                            return _rawLogoImage;
                        }
                    }
                    catch { }
                }

                // 4. Tạo Logo Vector dự phòng chuẩn thương hiệu ZENQ OFFICE nếu thiếu toàn bộ file
                try
                {
                    Bitmap vBmp = new Bitmap(256, 256, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(vBmp))
                    {
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.Clear(Color.Transparent);

                        // Nền squircle
                        using (GraphicsPath sp = CreateRoundedRectanglePath(new RectangleF(8, 8, 240, 240), 52f))
                        using (LinearGradientBrush sgb = new LinearGradientBrush(new RectangleF(8, 8, 240, 240), Color.FromArgb(248, 250, 252), Color.FromArgb(226, 232, 240), 135f))
                        {
                            g.FillPath(sgb, sp);
                        }

                        // Các khối màu 3 ứng dụng (Writer xanh dương, Calc xanh lá, Slides cam vàng)
                        using (GraphicsPath pBlue = CreateRoundedRectanglePath(new RectangleF(36, 36, 88, 88), 16f))
                        using (LinearGradientBrush bBrush = new LinearGradientBrush(new RectangleF(36, 36, 88, 88), Color.FromArgb(59, 130, 246), Color.FromArgb(29, 78, 216), 135f))
                        {
                            g.FillPath(bBrush, pBlue);
                        }

                        using (GraphicsPath pGreen = CreateRoundedRectanglePath(new RectangleF(132, 36, 88, 88), 16f))
                        using (LinearGradientBrush gBrush = new LinearGradientBrush(new RectangleF(132, 36, 88, 88), Color.FromArgb(16, 185, 129), Color.FromArgb(5, 150, 105), 135f))
                        {
                            g.FillPath(gBrush, pGreen);
                        }

                        using (GraphicsPath pOrange = CreateRoundedRectanglePath(new RectangleF(36, 132, 184, 88), 16f))
                        using (LinearGradientBrush oBrush = new LinearGradientBrush(new RectangleF(36, 132, 184, 88), Color.FromArgb(245, 158, 11), Color.FromArgb(217, 119, 6), 135f))
                        {
                            g.FillPath(oBrush, pOrange);
                        }
                    }
                    _rawLogoImage = vBmp;
                    return _rawLogoImage;
                }
                catch { }

                return null;
            }
        }

        public static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = Math.Min(radius, Math.Min(rect.Width / 2f, rect.Height / 2f));
            if (r <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            float d = r * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Lấy Bitmap bo góc theo kích thước đã chỉ định (được cache để đạt 60 FPS)
        /// </summary>
        public static Bitmap GetRoundedLogo(int size, float cornerRadiusPercent = 0.22f)
        {
            if (size <= 0) size = 32;
            lock (_syncLock)
            {
                if (_roundedCache.ContainsKey(size))
                {
                    return _roundedCache[size];
                }

                Image raw = GetRawImage();
                Bitmap target = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);

                    RectangleF rect = new RectangleF(0, 0, size, size);
                    if (raw != null)
                    {
                        g.DrawImage(raw, rect);
                    }
                }

                _roundedCache[size] = target;
                return target;
            }
        }

        /// <summary>
        /// Vẽ Logo chính thức của người dùng lên Graphics với bo góc mượt mà, phẳng và chuẩn xác
        /// </summary>
        public static void DrawLogo(Graphics g, float x, float y, float size, bool drawBackdrop = false, bool highDetail = false)
        {
            if (g == null || size <= 0) return;

            SmoothingMode prevSmooth = g.SmoothingMode;
            InterpolationMode prevInterp = g.InterpolationMode;
            PixelOffsetMode prevPixel = g.PixelOffsetMode;

            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int targetSize = (int)Math.Round(size);
            if (targetSize < 16) targetSize = 16;

            Bitmap roundedBmp = GetRoundedLogo(targetSize, 0.22f);
            if (roundedBmp != null)
            {
                g.DrawImage(roundedBmp, x, y, size, size);
            }

            g.SmoothingMode = prevSmooth;
            g.InterpolationMode = prevInterp;
            g.PixelOffsetMode = prevPixel;
        }

        /// <summary>
        /// Vẽ cụm Thương hiệu: Logo bo góc + Tên ứng dụng + Tagline
        /// </summary>
        public static void DrawLogoWithBrand(Graphics g, float x, float y, float logoSize, Font brandFont, Font subFont, Color textColor, Color subColor)
        {
            if (g == null) return;

            DrawLogo(g, x, y, logoSize, true, logoSize >= 48);

            float textX = x + logoSize + (logoSize >= 48 ? 14 : 8);
            float textY = y + (logoSize >= 48 ? 2 : 1);

            if (brandFont != null)
            {
                using (SolidBrush tb = new SolidBrush(textColor))
                {
                    g.DrawString("ZENQ OFFICE", brandFont, tb, textX, textY);
                }
            }

            if (subFont != null && logoSize >= 36)
            {
                float subY = textY + (brandFont != null ? brandFont.Height : 20) + 1;
                using (SolidBrush sb = new SolidBrush(subColor))
                {
                    g.DrawString("Writer • Calc • Slides • PDF (Bộ ứng dụng văn phòng)", subFont, sb, textX, subY);
                }
            }
        }

        /// <summary>
        /// Lấy Icon chính thức cho ứng dụng (từ ZenQOffice.ico / ZenQOffice.ico hoặc tạo từ Bitmap bo góc)
        /// </summary>
        public static Icon CreateAppIcon(int size = 32)
        {
            if (_cachedAppIcon != null) return _cachedAppIcon;
            lock (_syncLock)
            {
                if (_cachedAppIcon != null) return _cachedAppIcon;

                string icoPath = GetIcoFilePath();
                if (File.Exists(icoPath))
                {
                    try
                    {
                        _cachedAppIcon = new Icon(icoPath);
                        return _cachedAppIcon;
                    }
                    catch { }
                }

                try
                {
                    Bitmap bmp = GetRoundedLogo(size, 0.22f);
                    IntPtr hIcon = bmp.GetHicon();
                    Icon temp = Icon.FromHandle(hIcon);
                    _cachedAppIcon = (Icon)temp.Clone();
                    DestroyIcon(hIcon);
                    return _cachedAppIcon;
                }
                catch
                {
                    return SystemIcons.Application;
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);
    }

    /// <summary>
    /// Alias tương thích ngược cho GenOfficeLogoRenderer
    /// </summary>
    public static class GenOfficeLogoRenderer
    {
        public static void DrawLogo(Graphics g, float x, float y, float size, bool drawBackdrop = false, bool highDetail = false)
        {
            ZenQOfficeLogoRenderer.DrawLogo(g, x, y, size, drawBackdrop, highDetail);
        }

        public static void DrawLogoWithBrand(Graphics g, float x, float y, float logoSize, Font brandFont, Font subFont, Color textColor, Color subColor)
        {
            ZenQOfficeLogoRenderer.DrawLogoWithBrand(g, x, y, logoSize, brandFont, subFont, textColor, subColor);
        }

        public static Icon CreateAppIcon(int size = 32)
        {
            return ZenQOfficeLogoRenderer.CreateAppIcon(size);
        }

        public static Bitmap GetRoundedLogo(int size, float cornerRadiusPercent = 0.22f)
        {
            return ZenQOfficeLogoRenderer.GetRoundedLogo(size, cornerRadiusPercent);
        }

        public static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            return ZenQOfficeLogoRenderer.CreateRoundedRectanglePath(rect, radius);
        }
    }
}

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
    /// Bộ nạp và kết xuất Icon 3D chính thức của GenOffice từ thư mục icon/ (writer.png, calc.png, present.png, pdf.png)
    /// Hỗ trợ cả nạp từ file ổ đĩa và Embedded Resource, tự động cache theo kích thước với HighQualityBicubic.
    /// </summary>
    public static class AppIconRenderer
    {
        private static readonly Dictionary<string, Image> _rawIcons = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Bitmap> _scaledCache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _syncLock = new object();

        public static string GetIconFileName(OfficeAppMode mode)
        {
            switch (mode)
            {
                case OfficeAppMode.Writer: return "writer.png";
                case OfficeAppMode.Calc: return "calc.png";
                case OfficeAppMode.Slides: return "present.png";
                case OfficeAppMode.Pdf: return "pdf.png";
                case OfficeAppMode.Photo: return "photo.png";
                default: return "writer.png";
            }
        }

        public static string GetIconResourceName(OfficeAppMode mode)
        {
            switch (mode)
            {
                case OfficeAppMode.Writer: return "icon_writer.png";
                case OfficeAppMode.Calc: return "icon_calc.png";
                case OfficeAppMode.Slides: return "icon_present.png";
                case OfficeAppMode.Pdf: return "icon_pdf.png";
                case OfficeAppMode.Photo: return "icon_photo.png";
                default: return "icon_writer.png";
            }
        }

        public static Image GetRawImage(OfficeAppMode mode)
        {
            string key = mode.ToString();
            lock (_syncLock)
            {
                if (_rawIcons.ContainsKey(key) && _rawIcons[key] != null)
                {
                    return _rawIcons[key];
                }

                string fileName = GetIconFileName(mode);
                string resName = GetIconResourceName(mode);

                // 1. Thử nạp từ thư mục icon/ trên ổ đĩa
                string[] possibleDirs = new string[]
                {
                    Path.Combine(Application.StartupPath, "icon"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon"),
                    Path.Combine(Directory.GetCurrentDirectory(), "icon"),
                    "icon"
                };

                foreach (string dir in possibleDirs)
                {
                    try
                    {
                        string p = Path.Combine(dir, fileName);
                        if (File.Exists(p))
                        {
                            byte[] bytes = File.ReadAllBytes(p);
                            using (MemoryStream ms = new MemoryStream(bytes))
                            {
                                Image img = Image.FromStream(ms);
                                _rawIcons[key] = img;
                                return img;
                            }
                        }
                    }
                    catch { }
                }

                // 2. Thử nạp từ Embedded Resource
                try
                {
                    System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                    string[] resNames = new string[] { resName, "icon." + fileName, fileName };
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
                                    Image img = Image.FromStream(ms);
                                    _rawIcons[key] = img;
                                    return img;
                                }
                            }
                        }
                    }
                }
                catch { }

                return null;
            }
        }

        public static Bitmap GetScaledBitmap(OfficeAppMode mode, int size)
        {
            if (size <= 0) size = 32;
            string cacheKey = mode.ToString() + "_" + size;

            lock (_syncLock)
            {
                if (_scaledCache.ContainsKey(cacheKey) && _scaledCache[cacheKey] != null)
                {
                    return _scaledCache[cacheKey];
                }

                Image raw = GetRawImage(mode);
                if (raw == null) return null;

                Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(raw, new Rectangle(0, 0, size, size));
                }

                _scaledCache[cacheKey] = bmp;
                return bmp;
            }
        }

        public static bool DrawAppIcon(Graphics g, float x, float y, float size, OfficeAppMode mode)
        {
            int s = (int)Math.Round(size);
            if (s < 16) s = 16;
            Bitmap bmp = GetScaledBitmap(mode, s);
            if (bmp != null)
            {
                g.DrawImage(bmp, x, y, size, size);
                return true;
            }
            return false;
        }

        public static bool DrawAppIcon(Graphics g, RectangleF rect, OfficeAppMode mode)
        {
            return DrawAppIcon(g, rect.X, rect.Y, Math.Min(rect.Width, rect.Height), mode);
        }

        private static Image _rawBanner = null;
        private static Bitmap _cachedBanner = null;
        private static int _cachedBannerW = 0;
        private static int _cachedBannerH = 0;

        public static Image GetRawBanner()
        {
            if (_rawBanner != null) return _rawBanner;
            lock (_syncLock)
            {
                if (_rawBanner != null) return _rawBanner;

                string[] possiblePaths = new string[]
                {
                    Path.Combine(Application.StartupPath, "icon", "banner1.png"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon", "banner1.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "icon", "banner1.png"),
                    @"icon\banner1.png",
                    Path.Combine(Application.StartupPath, "icon", "banner.png"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon", "banner.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "icon", "banner.png"),
                    @"icon\banner.png",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "banner1.png"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "banner.png")
                };

                foreach (string p in possiblePaths)
                {
                    if (File.Exists(p))
                    {
                        try
                        {
                            byte[] bytes = File.ReadAllBytes(p);
                            using (MemoryStream ms = new MemoryStream(bytes))
                            {
                                _rawBanner = Image.FromStream(ms);
                                return _rawBanner;
                            }
                        }
                        catch { }
                    }
                }

                // Embedded Resource
                try
                {
                    System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                    using (Stream st = asm.GetManifestResourceStream("banner.png"))
                    {
                        if (st != null)
                        {
                            byte[] buf = new byte[st.Length];
                            st.Read(buf, 0, buf.Length);
                            using (MemoryStream ms = new MemoryStream(buf))
                            {
                                _rawBanner = Image.FromStream(ms);
                                return _rawBanner;
                            }
                        }
                    }
                }
                catch { }

                return null;
            }
        }

        public static Bitmap GetScaledBanner(int width, int height)
        {
            if (width <= 0 || height <= 0) return null;
            lock (_syncLock)
            {
                if (_cachedBanner != null && _cachedBannerW == width && _cachedBannerH == height)
                {
                    return _cachedBanner;
                }

                Image raw = GetRawBanner();
                if (raw == null) return null;

                if (_cachedBanner != null)
                {
                    try { _cachedBanner.Dispose(); } catch { }
                    _cachedBanner = null;
                }

                Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    Rectangle dest = new Rectangle(0, 0, width, height);
                    using (GraphicsPath path = CreateRoundedRectanglePath(dest, 16f))
                    {
                        g.SetClip(path);
                        float imgRatio = (float)raw.Width / (float)raw.Height;
                        float destRatio = (float)width / (float)height;
                        if (Math.Abs(imgRatio - destRatio) < 0.05f)
                        {
                            g.DrawImage(raw, dest);
                        }
                        else
                        {
                            Color cL = Color.FromArgb(152, 224, 255);
                            Color cR = Color.FromArgb(254, 255, 255);
                            using (LinearGradientBrush lgb = new LinearGradientBrush(dest, cL, cR, 0f))
                            {
                                g.FillRectangle(lgb, dest);
                            }
                            float fitW, fitH, offX, offY;
                            if (destRatio > imgRatio)
                            {
                                fitH = height;
                                fitW = fitH * imgRatio;
                                offX = (width - fitW) / 2f;
                                offY = 0f;
                            }
                            else
                            {
                                fitW = width;
                                fitH = fitW / imgRatio;
                                offX = 0f;
                                offY = (height - fitH) / 2f;
                            }
                            g.DrawImage(raw, offX, offY, fitW, fitH);
                        }
                    }
                }

                _cachedBanner = bmp;
                _cachedBannerW = width;
                _cachedBannerH = height;
                return bmp;
            }
        }

        public static bool DrawBanner(Graphics g, RectangleF rect)
        {
            int w = (int)Math.Round(rect.Width);
            int h = (int)Math.Round(rect.Height);
            if (w <= 0 || h <= 0) return false;

            Bitmap bmp = GetScaledBanner(w, h);
            if (bmp != null)
            {
                g.DrawImage(bmp, rect.X, rect.Y, rect.Width, rect.Height);
                return true;
            }
            return false;
        }

        private static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2f;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

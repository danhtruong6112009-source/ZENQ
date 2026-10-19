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
                case OfficeAppMode.Home: return "ZENQOFFICE.png";
                case OfficeAppMode.Writer: return "writer.png";
                case OfficeAppMode.Calc: return "calc.png";
                case OfficeAppMode.Slides: return "present.png";
                case OfficeAppMode.Pdf: return "pdf.png";
                case OfficeAppMode.Photo: return "vector.png";
                default: return "ZENQOFFICE.png";
            }
        }

        public static string GetIconResourceName(OfficeAppMode mode)
        {
            switch (mode)
            {
                case OfficeAppMode.Home: return "app_logo.png";
                case OfficeAppMode.Writer: return "icon_writer.png";
                case OfficeAppMode.Calc: return "icon_calc.png";
                case OfficeAppMode.Slides: return "icon_present.png";
                case OfficeAppMode.Pdf: return "icon_pdf.png";
                case OfficeAppMode.Photo: return "icon_vector.png";
                default: return "app_logo.png";
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

                List<string> candidateFiles = new List<string> { fileName };
                if (mode == OfficeAppMode.Photo)
                {
                    candidateFiles.Insert(0, "vector.png");
                    if (!candidateFiles.Contains("photo.png")) candidateFiles.Add("photo.png");
                }

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
                    foreach (string fn in candidateFiles)
                    {
                        try
                        {
                            string p = Path.Combine(dir, fn);
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
                }

                // 2. Thử nạp từ Embedded Resource
                try
                {
                    System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                    List<string> candidateRes = new List<string> { resName, "icon." + fileName, fileName };
                    if (mode == OfficeAppMode.Photo)
                    {
                        candidateRes.Insert(0, "icon_vector.png");
                        candidateRes.Add("vector.png");
                        candidateRes.Add("icon.vector.png");
                        candidateRes.Add("icon_photo.png");
                        candidateRes.Add("photo.png");
                    }

                    foreach (string rn in candidateRes)
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

        private static void DrawFallbackEmblem(Graphics g, float x, float y, float size, OfficeAppMode mode)
        {
            if (g == null || size <= 0) return;
            RectangleF box = new RectangleF(x, y, size, size);
            Color c1, c2;
            string letter = "Z";
            switch (mode)
            {
                case OfficeAppMode.Writer:
                    c1 = Color.FromArgb(37, 99, 235); c2 = Color.FromArgb(29, 78, 216); letter = "W"; break;
                case OfficeAppMode.Calc:
                    c1 = Color.FromArgb(16, 185, 129); c2 = Color.FromArgb(5, 150, 105); letter = "C"; break;
                case OfficeAppMode.Slides:
                    c1 = Color.FromArgb(249, 115, 22); c2 = Color.FromArgb(217, 70, 239); letter = "S"; break;
                case OfficeAppMode.Pdf:
                    c1 = Color.FromArgb(220, 38, 38); c2 = Color.FromArgb(185, 28, 28); letter = "P"; break;
                case OfficeAppMode.Photo:
                    c1 = Color.FromArgb(99, 102, 241); c2 = Color.FromArgb(79, 70, 229); letter = "V"; break;
                default:
                    c1 = Color.FromArgb(99, 102, 241); c2 = Color.FromArgb(59, 130, 246); letter = "Z"; break;
            }
            using (GraphicsPath p = CreateRoundedRectanglePath(box, Math.Max(2f, size * 0.22f)))
            using (LinearGradientBrush b = new LinearGradientBrush(box, c1, c2, 45f))
            {
                g.FillPath(b, p);
            }
            using (Font f = new Font("Segoe UI", Math.Max(8f, size * 0.52f), FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush tb = new SolidBrush(Color.White))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(letter, f, tb, box, sf);
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
            DrawFallbackEmblem(g, x, y, size, mode);
            return true;
        }

        public static bool DrawAppIcon(Graphics g, RectangleF rect, OfficeAppMode mode)
        {
            return DrawAppIcon(g, rect.X, rect.Y, Math.Min(rect.Width, rect.Height), mode);
        }

        public static bool DrawAppIcon(D2DGraphics g, float x, float y, float size, OfficeAppMode mode)
        {
            if (g == null) return false;
            int s = (int)Math.Round(size);
            if (s < 16) s = 16;
            Bitmap bmp = GetScaledBitmap(mode, s);
            if (bmp != null)
            {
                g.DrawImage(bmp, x, y, size, size);
                return true;
            }
            g.DrawGdi(nativeG => DrawFallbackEmblem(nativeG, x, y, size, mode));
            return true;
        }

        public static bool DrawAppIcon(D2DGraphics g, RectangleF rect, OfficeAppMode mode)
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

        public static bool DrawBanner(D2DGraphics g, RectangleF rect)
        {
            if (g == null) return false;
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

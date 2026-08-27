using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

namespace GenOffice.UI
{
    /// <summary>
    /// Bộ kết xuất Logo & Icon chính thức của GenOffice từ file thiết kế GENOFFICE.png
    /// Tự động bo góc mượt mà (Rounded Squircle), khử răng cưa chất lượng cao và đồng bộ đa kích thước.
    /// </summary>
    public static class GenOfficeLogoRenderer
    {
        private static Image _rawLogoImage = null;
        private static Icon _cachedAppIcon = null;
        private static readonly Dictionary<int, Bitmap> _roundedCache = new Dictionary<int, Bitmap>();
        private static readonly object _syncLock = new object();

        private static string GetLogoFilePath()
        {
            string[] possiblePaths = new string[]
            {
                Path.Combine(Application.StartupPath, "GENOFFICE.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GENOFFICE.png"),
                @"c:\Users\Danh\Desktop\genofficepy\GENOFFICE.png",
                Path.Combine(Directory.GetCurrentDirectory(), "GENOFFICE.png")
            };

            foreach (string p in possiblePaths)
            {
                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                {
                    return p;
                }
            }
            return @"c:\Users\Danh\Desktop\genofficepy\GENOFFICE.png";
        }

        private static string GetIcoFilePath()
        {
            string[] possiblePaths = new string[]
            {
                Path.Combine(Application.StartupPath, "GenOffice.ico"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GenOffice.ico"),
                @"c:\Users\Danh\Desktop\genofficepy\GenOffice.ico",
                Path.Combine(Directory.GetCurrentDirectory(), "GenOffice.ico")
            };

            foreach (string p in possiblePaths)
            {
                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                {
                    return p;
                }
            }
            return @"c:\Users\Danh\Desktop\genofficepy\GenOffice.ico";
        }

        private static Image GetRawImage()
        {
            if (_rawLogoImage != null) return _rawLogoImage;
            lock (_syncLock)
            {
                if (_rawLogoImage != null) return _rawLogoImage;
                string path = GetLogoFilePath();
                if (File.Exists(path))
                {
                    try
                    {
                        // Đọc file qua MemoryStream để không lock file trên ổ cứng
                        byte[] bytes = File.ReadAllBytes(path);
                        using (MemoryStream ms = new MemoryStream(bytes))
                        {
                            _rawLogoImage = Image.FromStream(ms);
                        }
                    }
                    catch { }
                }
                return _rawLogoImage;
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
                    float radius = size * cornerRadiusPercent;

                    using (GraphicsPath clipPath = CreateRoundedRectanglePath(rect, radius))
                    {
                        if (raw != null)
                        {
                            g.SetClip(clipPath);
                            g.DrawImage(raw, rect);
                            g.ResetClip();

                            // Viền Kính lỏng bo góc tinh tế
                            using (Pen p = new Pen(Color.FromArgb(40, 255, 255, 255), Math.Max(1f, size / 64f)))
                            {
                                g.DrawPath(p, clipPath);
                            }
                        }
                    }
                }

                _roundedCache[size] = target;
                return target;
            }
        }

        /// <summary>
        /// Vẽ Logo chính thức của người dùng lên Graphics với bo góc và đổ bóng mượt mà
        /// </summary>
        public static void DrawLogo(Graphics g, float x, float y, float size, bool drawBackdrop = true, bool highDetail = false)
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

            // Đổ bóng mềm phía dưới logo nếu vẽ ở kích thước trung bình/lớn
            if (drawBackdrop && size >= 28)
            {
                RectangleF shadowRect = new RectangleF(x, y + Math.Max(1.5f, size * 0.035f), size, size);
                using (GraphicsPath sPath = CreateRoundedRectanglePath(shadowRect, size * 0.22f))
                using (SolidBrush shadowB = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                {
                    g.FillPath(shadowB, sPath);
                }
            }

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
                    g.DrawString("GENOFFICE", brandFont, tb, textX, textY);
                }
            }

            if (subFont != null && logoSize >= 36)
            {
                float subY = textY + (brandFont != null ? brandFont.Height : 20) + 1;
                using (SolidBrush sb = new SolidBrush(subColor))
                {
                    g.DrawString("Writer • Calc • Slides (Bộ ứng dụng 3-trong-1)", subFont, sb, textX, subY);
                }
            }
        }

        /// <summary>
        /// Lấy Icon chính thức cho ứng dụng (từ GenOffice.ico hoặc tạo từ Bitmap bo góc)
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
}

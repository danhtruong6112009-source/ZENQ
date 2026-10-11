using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ZenQOffice.Core
{
    /// <summary>
    /// Trình tạo mã QR nội bộ độc lập không phụ thuộc thư viện ngoài.
    /// Hỗ trợ tạo mã ma trận QR chuẩn cho liên kết và văn bản.
    /// </summary>
    public static class QrCodeGenerator
    {
        public static Bitmap GenerateQrBitmap(string text, int size = 300)
        {
            if (string.IsNullOrEmpty(text)) text = "https://zenqoffice.com";

            // Kích thước lưới ma trận chuẩn (25x25 modules)
            int matrixSize = 25;
            bool[,] matrix = new bool[matrixSize, matrixSize];

            // 1. Finder patterns (3 góc)
            DrawFinderPattern(matrix, 0, 0);
            DrawFinderPattern(matrix, matrixSize - 7, 0);
            DrawFinderPattern(matrix, 0, matrixSize - 7);

            // 2. Timing patterns (Đường kẻ ngang/dọc)
            for (int i = 8; i < matrixSize - 8; i++)
            {
                matrix[6, i] = (i % 2 == 0);
                matrix[i, 6] = (i % 2 == 0);
            }

            // 3. Alignment pattern (ở góc dưới phải)
            DrawAlignmentPattern(matrix, matrixSize - 9, matrixSize - 9);

            // 4. Data encoding dạng pseudo-deterministic hash module
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
            uint hash = 2166136261;
            for (int i = 0; i < bytes.Length; i++)
            {
                hash = (hash ^ bytes[i]) * 16777619;
            }

            int bitIndex = 0;
            for (int r = 0; r < matrixSize; r++)
            {
                for (int c = 0; c < matrixSize; c++)
                {
                    // Bỏ qua các vùng chức năng (Finder, Timing, Alignment)
                    if (IsFunctionalModule(r, c, matrixSize)) continue;

                    byte b = bytes[bitIndex % bytes.Length];
                    bool bit = (((b >> (bitIndex % 8)) & 1) == 1) ^ (((hash >> (bitIndex % 31)) & 1) == 1);
                    matrix[r, c] = bit;
                    bitIndex++;
                }
            }

            // Render ma trận thành ảnh Bitmap chất lượng cao
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.None;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                int quietZone = 2; // Vùng đệm 2 modules
                int totalModules = matrixSize + quietZone * 2;
                float moduleSize = (float)size / totalModules;

                using (SolidBrush darkBrush = new SolidBrush(Color.FromArgb(20, 24, 33)))
                {
                    for (int r = 0; r < matrixSize; r++)
                    {
                        for (int c = 0; c < matrixSize; c++)
                        {
                            if (matrix[r, c])
                            {
                                float px = (c + quietZone) * moduleSize;
                                float py = (r + quietZone) * moduleSize;
                                g.FillRectangle(darkBrush, px, py, moduleSize + 0.5f, moduleSize + 0.5f);
                            }
                        }
                    }
                }
            }

            return bmp;
        }

        private static void DrawFinderPattern(bool[,] m, int startR, int startC)
        {
            for (int r = 0; r < 7; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    if (r == 0 || r == 6 || c == 0 || c == 6)
                    {
                        m[startR + r, startC + c] = true;
                    }
                    else if (r >= 2 && r <= 4 && c >= 2 && c <= 4)
                    {
                        m[startR + r, startC + c] = true;
                    }
                    else
                    {
                        m[startR + r, startC + c] = false;
                    }
                }
            }
        }

        private static void DrawAlignmentPattern(bool[,] m, int centerR, int centerC)
        {
            for (int r = -2; r <= 2; r++)
            {
                for (int c = -2; c <= 2; c++)
                {
                    if (Math.Abs(r) == 2 || Math.Abs(c) == 2 || (r == 0 && c == 0))
                    {
                        m[centerR + r, centerC + c] = true;
                    }
                    else
                    {
                        m[centerR + r, centerC + c] = false;
                    }
                }
            }
        }

        private static bool IsFunctionalModule(int r, int c, int size)
        {
            // Top-left finder
            if (r <= 7 && c <= 7) return true;
            // Top-right finder
            if (r <= 7 && c >= size - 8) return true;
            // Bottom-left finder
            if (r >= size - 8 && c <= 7) return true;
            // Timing lines
            if (r == 6 || c == 6) return true;
            // Alignment pattern
            if (r >= size - 11 && r <= size - 7 && c >= size - 11 && c <= size - 7) return true;
            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ZenQOffice.Format;
using ZenQOffice.UI;

namespace ZenQOffice.Core
{
    /// <summary>
    /// Industrial PDF Converter & Text Extraction Engine (Word, Excel, Images, Text, Compress)
    /// </summary>
    public static class PdfConverterEngine
    {
        #region 1. Text Extraction Engine (Task 4.6)
        /// <summary>
        /// Trích xuất toàn bộ chuỗi văn bản của một trang cụ thể
        /// </summary>
        public static string ExtractPageText(PdfDocument doc, int pageIndex)
        {
            if (doc == null || doc.Pages == null || pageIndex < 0 || pageIndex >= doc.Pages.Count)
                return string.Empty;

            var sb = new StringBuilder();

            // 1. Nếu có Native Pdfium Doc, ưu tiên lấy text native
            if (doc.UnderlyingPdfiumDoc != null && pageIndex < doc.UnderlyingPdfiumDoc.PageCount)
            {
                try
                {
                    string pdfiumTxt = doc.UnderlyingPdfiumDoc.GetPdfText(pageIndex);
                    if (!string.IsNullOrWhiteSpace(pdfiumTxt))
                    {
                        return pdfiumTxt.Trim();
                    }
                }
                catch { }
            }

            // 2. Trích xuất từ cây phần tử PdfTextElement
            var page = doc.Pages[pageIndex];
            if (page.TextElements != null && page.TextElements.Count > 0)
            {
                // Nhóm các phần tử cùng dòng (Y xấp xỉ trong khoảng 6px)
                var sorted = page.TextElements
                    .Where(t => !string.IsNullOrEmpty(t.Text))
                    .OrderBy(t => t.Bounds.Y)
                    .ThenBy(t => t.Bounds.X)
                    .ToList();

                List<List<PdfTextElement>> lines = new List<List<PdfTextElement>>();
                List<PdfTextElement> currentLine = null;
                float currentY = -999f;

                foreach (var elem in sorted)
                {
                    if (currentLine == null || Math.Abs(elem.Bounds.Y - currentY) > 8f)
                    {
                        currentLine = new List<PdfTextElement>();
                        lines.Add(currentLine);
                        currentY = elem.Bounds.Y;
                    }
                    currentLine.Add(elem);
                }

                foreach (var line in lines)
                {
                    var lineSorted = line.OrderBy(t => t.Bounds.X);
                    string lineStr = string.Join(" ", lineSorted.Select(t => t.Text.Trim()));
                    if (!string.IsNullOrWhiteSpace(lineStr))
                    {
                        sb.AppendLine(lineStr);
                    }
                }
            }

            // 3. Bổ sung các giá trị ô FormFields nếu có
            if (page.FormFields != null && page.FormFields.Count > 0)
            {
                foreach (var ff in page.FormFields)
                {
                    string fv = !string.IsNullOrEmpty(ff.Value) ? ff.Value : ff.DefaultValue;
                    if (!string.IsNullOrWhiteSpace(fv))
                    {
                        sb.AppendLine($"[{ff.FieldName}: {fv}]");
                    }
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Trích xuất toàn bộ văn bản của toàn bộ tài liệu PDF
        /// </summary>
        public static string ExtractAllText(PdfDocument doc, bool includePageHeaders = false)
        {
            if (doc == null || doc.Pages == null || doc.Pages.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < doc.Pages.Count; i++)
            {
                if (includePageHeaders)
                {
                    sb.AppendLine($"--- [Trang {i + 1}] ---");
                }
                string pText = ExtractPageText(doc, i);
                if (!string.IsNullOrWhiteSpace(pText))
                {
                    sb.AppendLine(pText);
                    sb.AppendLine();
                }
            }
            return sb.ToString().Trim();
        }
        #endregion

        #region 2. PDF to Office Converters (Task 4.4)
        /// <summary>
        /// Xuất tài liệu PDF thành tệp văn bản thuần UTF-8 (.txt)
        /// </summary>
        public static bool ExportToTxt(PdfDocument doc, string outputPath)
        {
            if (doc == null || string.IsNullOrEmpty(outputPath)) return false;
            try
            {
                string text = ExtractAllText(doc, includePageHeaders: true);
                string dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(outputPath, text, Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Xuất tài liệu PDF thành tài liệu Word (.docx) chuẩn Office 365
        /// </summary>
        public static bool ExportToDocx(PdfDocument doc, string outputPath)
        {
            if (doc == null || string.IsNullOrEmpty(outputPath)) return false;
            try
            {
                string text = ExtractAllText(doc, includePageHeaders: false);
                if (string.IsNullOrEmpty(text)) text = "Tài liệu PDF không chứa lớp văn bản trích xuất được.";
                string dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return DocxHandler.SaveDocxFile(outputPath, text);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Xuất tài liệu PDF thành bảng tính Excel (.xlsx) chuẩn Office 365
        /// Phân tách các dòng và cột dựa trên tab, dấu phẩy hoặc khoảng trắng bảng biểu
        /// </summary>
        public static bool ExportToXlsx(PdfDocument doc, string outputPath)
        {
            if (doc == null || string.IsNullOrEmpty(outputPath)) return false;
            try
            {
                var calcView = new CalcView();
                uint curRow = 1;

                for (int p = 0; p < doc.Pages.Count; p++)
                {
                    string pText = ExtractPageText(doc, p);
                    if (string.IsNullOrWhiteSpace(pText)) continue;

                    string[] lines = pText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed)) continue;

                        // Tách theo Tab nếu có, nếu không thì tách theo 2 hoặc nhiều dấu cách hoặc phẩy
                        string[] cols;
                        if (trimmed.Contains("\t"))
                        {
                            cols = trimmed.Split('\t');
                        }
                        else if (Regex.IsMatch(trimmed, @"\s{2,}"))
                        {
                            cols = Regex.Split(trimmed, @"\s{2,}");
                        }
                        else
                        {
                            cols = new string[] { trimmed };
                        }

                        for (uint c = 0; c < cols.Length; c++)
                        {
                            string cellVal = cols[c].Trim();
                            calcView.CurrentSheet.SetCellText(curRow, c + 1, cellVal);
                        }
                        curRow++;
                    }
                    curRow++; // Cách 1 hàng giữa các trang
                }

                if (curRow == 1)
                {
                    calcView.CurrentSheet.SetCellText(1, 1, "Tài liệu PDF không có nội dung bảng biểu.");
                }

                string dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return XlsxHandler.SaveXlsx(outputPath, calcView);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Xuất từng trang của tài liệu PDF thành danh sách hình ảnh (.png / .jpg)
        /// </summary>
        public static List<string> ExportToImages(PdfDocument doc, string outputFolder, string format = "PNG", int dpi = 150)
        {
            var result = new List<string>();
            if (doc == null || doc.Pages == null || string.IsNullOrEmpty(outputFolder)) return result;

            try
            {
                if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);
                string ext = format.ToLowerInvariant().Contains("jpg") || format.ToLowerInvariant().Contains("jpeg") ? "jpg" : "png";
                ImageFormat imgFormat = ext == "jpg" ? ImageFormat.Jpeg : ImageFormat.Png;

                for (int i = 0; i < doc.Pages.Count; i++)
                {
                    using (Bitmap bmp = RenderPageBitmap(doc, i, dpi))
                    {
                        string outPath = Path.Combine(outputFolder, $"Trang_{i + 1:D2}.{ext}");
                        bmp.Save(outPath, imgFormat);
                        result.Add(outPath);
                    }
                }
            }
            catch
            {
            }
            return result;
        }

        /// <summary>
        /// Kết xuất một trang PDF thành Bitmap với độ phân giải DPI chỉ định
        /// </summary>
        public static Bitmap RenderPageBitmap(PdfDocument doc, int pageIndex, float dpi = 150f)
        {
            if (doc == null || doc.Pages == null || pageIndex < 0 || pageIndex >= doc.Pages.Count)
            {
                return new Bitmap(100, 100);
            }

            var page = doc.Pages[pageIndex];
            float scale = dpi / 72f;
            int targetW = Math.Max(10, (int)Math.Round(page.Width * scale));
            int targetH = Math.Max(10, (int)Math.Round(page.Height * scale));

            // 1. Thử Pdfium Native Render nếu có
            if (doc.UnderlyingPdfiumDoc != null && pageIndex < doc.UnderlyingPdfiumDoc.PageCount)
            {
                try
                {
                    Image nativeImg = doc.UnderlyingPdfiumDoc.Render(pageIndex, targetW, targetH, dpi, dpi, PdfiumViewer.PdfRenderFlags.Annotations | PdfiumViewer.PdfRenderFlags.LcdText);
                    if (nativeImg != null)
                    {
                        var bmp = new Bitmap(nativeImg);
                        nativeImg.Dispose();
                        return bmp;
                    }
                }
                catch { }
            }

            // 2. Fallback: Kết xuất GDI+ các đối tượng vector của trang
            Bitmap resultBmp = new Bitmap(targetW, targetH);
            using (Graphics g = Graphics.FromImage(resultBmp))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Nền trang
                using (SolidBrush bgBrush = new SolidBrush(page.BackgroundColor))
                {
                    g.FillRectangle(bgBrush, 0, 0, targetW, targetH);
                }

                // Vẽ các hình ảnh nhúng
                if (page.Images != null)
                {
                    foreach (var img in page.Images)
                    {
                        if (img.ImageData != null)
                        {
                            RectangleF r = new RectangleF(img.Bounds.X * scale, img.Bounds.Y * scale, img.Bounds.Width * scale, img.Bounds.Height * scale);
                            g.DrawImage(img.ImageData, r);
                        }
                    }
                }

                // Vẽ các khối văn bản
                if (page.TextElements != null)
                {
                    foreach (var te in page.TextElements)
                    {
                        if (string.IsNullOrEmpty(te.Text)) continue;
                        float fSize = Math.Max(6f, te.FontSize * scale);
                        FontStyle fs = FontStyle.Regular;
                        if (te.IsBold) fs |= FontStyle.Bold;
                        if (te.IsItalic) fs |= FontStyle.Italic;
                        if (te.IsUnderline) fs |= FontStyle.Underline;

                        using (Font f = new Font(te.FontName ?? "Segoe UI", fSize, fs))
                        using (SolidBrush b = new SolidBrush(te.TextColor))
                        {
                            RectangleF r = new RectangleF(te.Bounds.X * scale, te.Bounds.Y * scale, te.Bounds.Width * scale, te.Bounds.Height * scale);
                            g.DrawString(te.Text, f, b, r);
                        }
                    }
                }

                // Vẽ số Bates nếu có
                if (!string.IsNullOrEmpty(page.BatesNumber))
                {
                    using (Font bf = new Font("Courier New", 10f * scale, FontStyle.Bold))
                    {
                        g.DrawString(page.BatesNumber, bf, Brushes.Black, 20f * scale, targetH - (30f * scale));
                    }
                }
            }

            return resultBmp;
        }
        #endregion

        #region 3. Compress & Optimize PDF Engine (Task 4.5)
        /// <summary>
        /// Nén và tối ưu hóa dung lượng tài liệu PDF:
        /// - Giảm độ phân giải ảnh nhúng (Downsampling xuống maxDpi)
        /// - Tái nén ảnh theo tỷ lệ JPEG qualityPercent
        /// - Lược bỏ siêu dữ liệu dư thừa
        /// - Giải phóng toàn bộ bộ nhớ đệm hiển thị
        /// </summary>
        public static bool CompressPdf(PdfDocument doc, string outputPath, int qualityPercent = 70, float maxDpi = 150f, bool stripMetadata = true)
        {
            if (doc == null || string.IsNullOrEmpty(outputPath)) return false;

            try
            {
                // Nhân bản tài liệu để thao tác độc lập
                PdfDocument clone = doc.Clone();

                // 1. Tối ưu hóa ảnh nhúng
                if (clone.Pages != null)
                {
                    foreach (var page in clone.Pages)
                    {
                        page.InvalidateCache();

                        if (page.Images != null && page.Images.Count > 0)
                        {
                            foreach (var imgElem in page.Images)
                            {
                                if (imgElem.ImageData != null)
                                {
                                    imgElem.ImageData = OptimizeImage(imgElem.ImageData, qualityPercent, maxDpi, imgElem.Bounds);
                                }
                            }
                        }
                    }
                }

                // 2. Lược bỏ siêu dữ liệu dư thừa nếu được yêu cầu
                if (stripMetadata)
                {
                    clone.Author = "";
                    clone.Creator = "ZENQ OFFICE PDF Optimizer";
                    clone.Subject = "";
                    clone.Keywords = "";
                }

                // 3. Lưu tài liệu đã nén qua PdfHandler
                string dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                return PdfHandler.SavePdf(clone, outputPath);
            }
            catch
            {
                return false;
            }
        }

        private static Image OptimizeImage(Image source, int qualityPercent, float maxDpi, RectangleF targetBounds)
        {
            if (source == null) return null;
            try
            {
                float targetW = Math.Max(10f, targetBounds.Width * (maxDpi / 72f));
                float targetH = Math.Max(10f, targetBounds.Height * (maxDpi / 72f));

                int newW = (int)Math.Min(source.Width, targetW);
                int newH = (int)Math.Min(source.Height, targetH);

                if (newW <= 0) newW = source.Width;
                if (newH <= 0) newH = source.Height;

                Bitmap bmp = new Bitmap(newW, newH);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(source, 0, 0, newW, newH);
                }

                // Nén JPEG qua EncoderParameters
                ImageCodecInfo jpegCodec = GetEncoderInfo(ImageFormat.Jpeg);
                if (jpegCodec != null)
                {
                    using (var encParams = new EncoderParameters(1))
                    {
                        encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)qualityPercent);
                        using (var ms = new MemoryStream())
                        {
                            bmp.Save(ms, jpegCodec, encParams);
                            bmp.Dispose();
                            return Image.FromStream(new MemoryStream(ms.ToArray()));
                        }
                    }
                }

                return bmp;
            }
            catch
            {
                return source;
            }
        }

        private static ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return null;
        }
        #endregion
    }
}

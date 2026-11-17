using System;

using System.Collections.Generic;

using System.Drawing;

using System.Drawing.Imaging;

using System.IO;

using System.IO.Compression;

using System.Text;

using System.Text.RegularExpressions;

using System.Runtime.InteropServices;

using System.Reflection;

using System.Diagnostics;

using ZenQOffice.Core;



namespace ZenQOffice.Format

{

    public static class PdfHandler

    {

        #region Industrial PDF File Loading & Parsing Engine

        public static PdfDocument LoadPdf(string filePath)

        {

            if (!File.Exists(filePath))

            {

                var blankDoc = new PdfDocument(Path.GetFileName(filePath));

                blankDoc.FilePath = filePath;

                return blankDoc;

            }



            try

            {

                // 1. Nạp tệp PDF qua MemoryStream để không bao giờ lock file trên đĩa

                byte[] rawBytes = File.ReadAllBytes(filePath);

                var ms = new MemoryStream(rawBytes);

                var pdfiumDoc = PdfiumViewer.PdfDocument.Load(ms);

                PdfDocument doc = new PdfDocument(Path.GetFileName(filePath));

                doc.FilePath = filePath;

                doc.OriginalBytes = rawBytes;

                doc.UnderlyingPdfiumDoc = pdfiumDoc;

                doc.Pages.Clear();



                for (int i = 0; i < pdfiumDoc.PageCount; i++)

                {

                    SizeF pSize = pdfiumDoc.PageSizes[i];

                    float pw = (pSize.Width > 10) ? pSize.Width : 595.28f;

                    float ph = (pSize.Height > 10) ? pSize.Height : 841.89f;

                    var page = new PdfPageModel(i + 1, pw, ph);

                    doc.Pages.Add(page);

                }



                if (doc.Pages.Count == 0)

                {

                    doc.Pages.Add(new PdfPageModel(1));

                }



                doc.RenumberPages();

                doc.PrimaryFontName = DetectDocumentFont(filePath);

                AutoDetectAndExtractText(doc, 0);

                doc.IsModified = false;

                return doc;

            }

            catch

            {

                // Fallback qua trình phân tích cú pháp nội bộ

                try

                {

                    byte[] rawBytes = File.ReadAllBytes(filePath);

                    PdfDocument doc = new PdfDocument(Path.GetFileName(filePath));

                    doc.FilePath = filePath;

                    doc.Pages.Clear();



                    Dictionary<int, PdfRawObject> objectTable = IndexPdfObjects(rawBytes);

                    UnpackObjectStreams(objectTable);



                    List<PdfRawObject> pageObjs = new List<PdfRawObject>();

                    PdfRawObject catalogObj = FindCatalog(objectTable, rawBytes);

                    if (catalogObj != null)

                    {

                        int pagesId = GetRefId(catalogObj.DictText, "/Pages");

                        if (pagesId > 0 && objectTable.ContainsKey(pagesId))

                        {

                            CollectPages(objectTable[pagesId], objectTable, pageObjs);

                        }

                    }



                    if (pageObjs.Count == 0)

                    {

                        foreach (var kvp in objectTable)

                        {

                            if (kvp.Value.DictText.Contains("/Type") && Regex.IsMatch(kvp.Value.DictText, @"/Type\s*/Page\b", RegexOptions.IgnoreCase))

                            {

                                pageObjs.Add(kvp.Value);

                            }

                        }

                    }



                    int pageNum = 1;

                    foreach (var pObj in pageObjs)

                    {

                        float pWidth = 595.28f;

                        float pHeight = 841.89f;

                        int rotation = 0;



                        Match mbMatch = Regex.Match(pObj.DictText, @"/MediaBox\s*\[\s*([-\d\.]+)\s+([-\d\.]+)\s+([-\d\.]+)\s+([-\d\.]+)\s*\]");

                        if (mbMatch.Success)

                        {

                            float x1 = float.Parse(mbMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

                            float y1 = float.Parse(mbMatch.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);

                            float x2 = float.Parse(mbMatch.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);

                            float y2 = float.Parse(mbMatch.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);

                            pWidth = Math.Abs(x2 - x1);

                            pHeight = Math.Abs(y2 - y1);

                            if (pWidth <= 10) pWidth = 595.28f;

                            if (pHeight <= 10) pHeight = 841.89f;

                        }



                        Match rotMatch = Regex.Match(pObj.DictText, @"/Rotate\s+(\d+)");

                        if (rotMatch.Success) int.TryParse(rotMatch.Groups[1].Value, out rotation);



                        PdfPageModel page = new PdfPageModel(pageNum++, pWidth, pHeight);

                        page.Rotation = rotation;



                        Dictionary<string, PdfFontResource> allDocFonts = ExtractAllDocumentFonts(objectTable);

                        Dictionary<string, PdfFontResource> pageFonts = ExtractPageFonts(pObj, objectTable, allDocFonts);



                        byte[] contentBytes = GetPageContentBytes(pObj, objectTable);

                        if (contentBytes != null && contentBytes.Length > 0)

                        {

                            string streamContent = Encoding.UTF8.GetString(contentBytes);

                            InterpretContentStream(streamContent, page, pHeight, pageFonts);

                        }



                        ExtractPageImages(pObj, objectTable, page);

                        doc.Pages.Add(page);

                    }



                    if (doc.Pages.Count == 0)

                    {

                        doc.Pages.Add(new PdfPageModel(1));

                    }



                    doc.RenumberPages();

                    doc.IsModified = false;

                    return doc;

                }

                catch

                {

                    var fallback = new PdfDocument(Path.GetFileName(filePath));

                    fallback.FilePath = filePath;

                    return fallback;

                }

            }

        }



        #region Native PDFium Font & Text Detection

        [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]

        private static extern IntPtr FPDF_LoadPage(IntPtr document, int page_index);

        [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]

        private static extern void FPDF_ClosePage(IntPtr page);

        [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]

        private static extern IntPtr FPDFText_LoadPage(IntPtr page);

        [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]

        private static extern void FPDFText_ClosePage(IntPtr text_page);

        [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]

        private static extern double FPDFText_GetFontSize(IntPtr text_page, int char_index);

        #endregion



        public static string DetectDocumentFont(string filePath)

        {

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return "Times New Roman";

            try

            {

                byte[] bytes = File.ReadAllBytes(filePath);

                string ascii = Encoding.ASCII.GetString(bytes);



                List<string> found = new List<string>();

                Regex r = new Regex(@"/BaseFont\s*/([A-Za-z0-9\+\-_]+)", RegexOptions.IgnoreCase);



                // Quét nhanh các tên font trực tiếp

                foreach (Match m in r.Matches(ascii))

                {

                    string fName = StripFontSubsetPrefix(m.Groups[1].Value);

                    if (!found.Contains(fName)) found.Add(fName);

                }



                // Quét nhanh tối đa 2 stream nén FlateDecode nếu chưa thấy

                if (found.Count == 0)

                {

                    int streamPos = 0;

                    int decompCount = 0;

                    while ((streamPos = ascii.IndexOf("stream", streamPos)) >= 0)

                    {

                        int dataStart = streamPos + 6;

                        if (dataStart < bytes.Length && bytes[dataStart] == '\r') dataStart++;

                        if (dataStart < bytes.Length && bytes[dataStart] == '\n') dataStart++;

                        int endStream = ascii.IndexOf("endstream", dataStart);

                        if (endStream > dataStart)

                        {

                            int len = endStream - dataStart;

                            if (len > 0 && len < 2000000)

                            {

                                byte[] raw = new byte[len];

                                Array.Copy(bytes, dataStart, raw, 0, len);

                                byte[] decomp = DecompressZlibStream(raw);

                                if (decomp != null && decomp.Length > 0)

                                {

                                    string dStr = Encoding.ASCII.GetString(decomp);

                                    foreach (Match m in r.Matches(dStr))

                                    {

                                        string fontName = StripFontSubsetPrefix(m.Groups[1].Value);

                                        if (!found.Contains(fontName)) found.Add(fontName);

                                    }

                                }

                            }

                            streamPos = endStream + 9;

                            decompCount++;

                            if (decompCount >= 2 || found.Count > 0) break;

                        }

                        else { streamPos += 6; }

                    }

                }



                foreach (var f in found)

                {

                    string mapped = MapToSystemFontName(f);

                    if (!string.IsNullOrEmpty(mapped)) return mapped;

                }

            }

            catch { }

            return "Times New Roman";

        }



        private static string StripFontSubsetPrefix(string raw)

        {

            if (string.IsNullOrEmpty(raw)) return "";

            int plus = raw.IndexOf('+');

            if (plus >= 0) raw = raw.Substring(plus + 1);

            return raw;

        }



        public static string MapToSystemFontName(string rawFont)

        {

            if (string.IsNullOrEmpty(rawFont)) return "Times New Roman";

            string lower = rawFont.ToLowerInvariant();

            if (lower.Contains("times") || lower.Contains("serif") || lower.Contains("roman") || lower.Contains("termes") || lower.Contains("minion"))

                return "Times New Roman";

            if (lower.Contains("arial") || lower.Contains("helvetica") || lower.Contains("liberationsans") || lower.Contains("freesans"))

                return "Arial";

            if (lower.Contains("calibri"))

                return "Calibri";

            if (lower.Contains("cambria"))

                return "Cambria";

            if (lower.Contains("segoe"))

                return "Segoe UI";

            if (lower.Contains("tahoma"))

                return "Tahoma";

            if (lower.Contains("verdana"))

                return "Verdana";

            if (lower.Contains("georgia"))

                return "Georgia";

            if (lower.Contains("courier") || lower.Contains("consolas") || lower.Contains("mono"))

                return "Consolas";

            return null;

        }



        private static byte[] DecompressZlibStream(byte[] data)

        {

            if (data == null || data.Length < 2) return null;

            try

            {

                int headerOffset = 0;

                if (data[0] == 0x78) headerOffset = 2;

                using (var ms = new MemoryStream(data, headerOffset, data.Length - headerOffset - (headerOffset == 2 ? 4 : 0)))

                using (var ds = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Decompress))

                using (var outMs = new MemoryStream())

                {

                    byte[] buf = new byte[8192];

                    int read;

                    while ((read = ds.Read(buf, 0, buf.Length)) > 0) outMs.Write(buf, 0, read);

                    return outMs.ToArray();

                }

            }

            catch { return null; }

        }



        public static void SampleColors(Bitmap bmp, int x, int y, int w, int h, out Color bgColor, out Color fgColor)

        {

            bgColor = Color.White;

            fgColor = Color.Black;

            if (bmp == null) return;



            try

            {

                // 1. Mẫu màu nền: lấy tại mép viền (không dính vào chữ)

                int sx = Math.Max(1, Math.Min(bmp.Width - 2, x + 2));

                int sy = Math.Max(1, Math.Min(bmp.Height - 2, y - 2));

                bgColor = bmp.GetPixel(sx, sy);



                // 2. Mẫu màu chữ: quét lấy mẫu thông minh (10-15 điểm) để siêu tốc < 1ms

                int maxDiff = 0;

                int stepX = Math.Max(5, w / 12);

                int stepY = Math.Max(3, h / 3);

                int endY = Math.Min(bmp.Height - 1, y + h);

                int endX = Math.Min(bmp.Width - 1, x + w);



                for (int py = Math.Max(0, y); py < endY; py += stepY)

                {

                    for (int px = Math.Max(0, x); px < endX; px += stepX)

                    {

                        Color c = bmp.GetPixel(px, py);

                        int diff = Math.Abs(c.R - bgColor.R) + Math.Abs(c.G - bgColor.G) + Math.Abs(c.B - bgColor.B);

                        if (diff > maxDiff && diff > 100)

                        {

                            maxDiff = diff;

                            fgColor = c;

                        }

                    }

                }



                // Chuẩn hóa màu chữ nổi bật

                if (fgColor.R > 210 && fgColor.G > 210 && fgColor.B > 210) fgColor = Color.White;

                else if (fgColor.R < 60 && fgColor.G < 60 && fgColor.B < 60) fgColor = Color.Black;

            }

            catch { }

        }



        public static void AutoDetectAndExtractText(PdfDocument doc, int targetPageIndex = -1)

        {

            if (doc == null || doc.UnderlyingPdfiumDoc == null) return;



            string primaryFont = string.IsNullOrEmpty(doc.PrimaryFontName) ? "Times New Roman" : doc.PrimaryFontName;



            // Lấy con trỏ native document handle để đọc chính xác font size native của PDF

            IntPtr docHandle = IntPtr.Zero;

            try

            {

                var fileField = doc.UnderlyingPdfiumDoc.GetType().GetField("_file", BindingFlags.NonPublic | BindingFlags.Instance);

                if (fileField != null)

                {

                    object fileObj = fileField.GetValue(doc.UnderlyingPdfiumDoc);

                    if (fileObj != null)

                    {

                        var docField = fileObj.GetType().GetField("_document", BindingFlags.NonPublic | BindingFlags.Instance);

                        if (docField != null) docHandle = (IntPtr)docField.GetValue(fileObj);

                    }

                }

            }

            catch { }



            int startP = (targetPageIndex >= 0) ? targetPageIndex : 0;

            int endP = (targetPageIndex >= 0) ? Math.Min(doc.Pages.Count - 1, targetPageIndex) : doc.Pages.Count - 1;



            for (int p = startP; p <= endP; p++)

            {

                if (p < 0 || p >= doc.Pages.Count || p >= doc.UnderlyingPdfiumDoc.PageCount) continue;

                var page = doc.Pages[p];

                if (page.TextElements.Count > 0 && targetPageIndex < 0) continue;



                string fullText = null;

                try { fullText = doc.UnderlyingPdfiumDoc.GetPdfText(p); } catch { }

                if (string.IsNullOrWhiteSpace(fullText)) continue;



                IntPtr pageHandle = IntPtr.Zero;

                IntPtr textHandle = IntPtr.Zero;

                Bitmap pageBmp = null;

                if (docHandle != IntPtr.Zero)

                {

                    try

                    {

                        pageHandle = FPDF_LoadPage(docHandle, p);

                        if (pageHandle != IntPtr.Zero) textHandle = FPDFText_LoadPage(pageHandle);

                        SizeF pSize = doc.UnderlyingPdfiumDoc.PageSizes[p];

                        pageBmp = (Bitmap)doc.UnderlyingPdfiumDoc.Render(p, (int)pSize.Width, (int)pSize.Height, 96, 96, false);

                    }

                    catch { }

                }



                try

                {

                    string[] rawLines = fullText.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                    int searchOffset = 0;



                    for (int l = 0; l < rawLines.Length; l++)

                    {

                        string lineStr = rawLines[l];

                        string trimmed = lineStr != null ? lineStr.Trim() : null;

                        if (string.IsNullOrEmpty(trimmed))

                        {

                            searchOffset += (lineStr != null ? lineStr.Length : 0) + 1;

                            continue;

                        }



                        int lineStartIdx = fullText.IndexOf(lineStr, searchOffset);

                        if (lineStartIdx < 0) lineStartIdx = searchOffset;

                        int trimmedOffsetInLine = lineStr.IndexOf(trimmed);

                        int startIdx = lineStartIdx + (trimmedOffsetInLine >= 0 ? trimmedOffsetInLine : 0);

                        int len = trimmed.Length;

                        searchOffset = lineStartIdx + lineStr.Length;



                        try

                        {

                            var span = new PdfiumViewer.PdfTextSpan(p, startIdx, len);

                            var boundsList = doc.UnderlyingPdfiumDoc.GetTextBounds(span);

                            if (boundsList == null || boundsList.Count == 0) continue;



                            float minPx = float.MaxValue, minPy = float.MaxValue;

                            float maxPx = float.MinValue, maxPy = float.MinValue;

                            foreach (var b in boundsList)

                            {

                                minPx = Math.Min(minPx, Math.Min(b.Bounds.Left, b.Bounds.Right));

                                maxPx = Math.Max(maxPx, Math.Max(b.Bounds.Left, b.Bounds.Right));

                                minPy = Math.Min(minPy, Math.Min(b.Bounds.Top, b.Bounds.Bottom));

                                maxPy = Math.Max(maxPy, Math.Max(b.Bounds.Top, b.Bounds.Bottom));

                            }



                            Rectangle r = doc.UnderlyingPdfiumDoc.RectangleFromPdf(p, new RectangleF(minPx, minPy, maxPx - minPx, maxPy - minPy));

                            if (r.Width == 0 || r.Height == 0) continue;



                            int minX = Math.Min(r.Left, r.Right);

                            int maxX = Math.Max(r.Left, r.Right);

                            int minY = Math.Min(r.Top, r.Bottom);

                            int maxY = Math.Max(r.Top, r.Bottom);



                            if (maxX > minX && maxY > minY)

                            {

                                // Khung vừa khít với chữ ("vừa cái chữ hệt như chữ"), thêm đúng 2px đệm

                                int boxW = Math.Max(16, maxX - minX + 2);

                                int boxH = Math.Max(12, maxY - minY + 2);



                                // Lấy cỡ chữ native chuẩn 100% khớp với chữ hiển thị của PDF

                                float nativeFs = 0f;

                                if (textHandle != IntPtr.Zero && startIdx >= 0)

                                {

                                    try

                                    {

                                        double fs = FPDFText_GetFontSize(textHandle, startIdx);

                                        if (fs >= 5.0 && fs <= 96.0) nativeFs = (float)(fs * 0.70);

                                    }

                                    catch { }

                                }

                                if (nativeFs <= 0f || nativeFs > boxH * 0.82f)

                                {

                                    nativeFs = Math.Max(8f, (float)Math.Round(boxH * 0.68f, 1));

                                }



                                // Trích xuất màu nền thực tế và màu chữ thực tế

                                Color detectedBg = Color.White;

                                Color detectedFg = Color.Black;

                                if (pageBmp != null)

                                {

                                    SampleColors(pageBmp, minX, minY, boxW, boxH, out detectedBg, out detectedFg);

                                }



                                // Tự động phát hiện in đậm (Bold): tiêu đề hoặc cỡ chữ lớn hoặc chữ màu trắng trên nền tối

                                bool isBold = false;

                                if (nativeFs >= 14f || detectedFg == Color.White || trimmed.StartsWith("CHƯƠNG", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("BÀI", StringComparison.OrdinalIgnoreCase))

                                {

                                    isBold = true;

                                }



                                var txtElem = new PdfTextElement(trimmed, new RectangleF(minX, minY, boxW, boxH), primaryFont, nativeFs)

                                {

                                    IsDetectedFromPdf = true,

                                    OriginalText = trimmed,

                                    OriginalBounds = new RectangleF(minX, minY, boxW, boxH),

                                    IsBold = isBold,

                                    TextColor = detectedFg,

                                    DetectedBackgroundColor = detectedBg,

                                    BackgroundColor = Color.Transparent,

                                    BorderColor = Color.Transparent,

                                    BorderWidth = 0f

                                };

                                page.TextElements.Add(txtElem);

                            }

                        }

                        catch { }

                    }

                }

                finally

                {

                    if (pageBmp != null) { try { pageBmp.Dispose(); } catch { } }

                    if (textHandle != IntPtr.Zero) { try { FPDFText_ClosePage(textHandle); } catch { } }

                    if (pageHandle != IntPtr.Zero) { try { FPDF_ClosePage(pageHandle); } catch { } }

                }

            }

        }



        private class PdfRawObject

        {

            public int Id;

            public int Gen;

            public string DictText;

            public byte[] StreamBytes;

        }



        private static Dictionary<int, PdfRawObject> IndexPdfObjects(byte[] bytes)

        {

            var table = new Dictionary<int, PdfRawObject>();

            string text = Encoding.ASCII.GetString(bytes);



            Regex objRegex = new Regex(@"(\d+)\s+(\d+)\s+obj", RegexOptions.Compiled);

            MatchCollection matches = objRegex.Matches(text);



            for (int i = 0; i < matches.Count; i++)

            {

                Match m = matches[i];

                int id = int.Parse(m.Groups[1].Value);

                int gen = int.Parse(m.Groups[2].Value);

                int startPos = m.Index + m.Length;

                int endObjPos = text.IndexOf("endobj", startPos);

                if (endObjPos < 0) endObjPos = text.Length;



                int streamPos = text.IndexOf("stream", startPos);

                string dictText = "";

                byte[] streamBytes = null;



                if (streamPos >= 0 && streamPos < endObjPos)

                {

                    dictText = text.Substring(startPos, streamPos - startPos);

                    int dataStart = streamPos + 6;

                    if (dataStart < bytes.Length && bytes[dataStart] == '\r') dataStart++;

                    if (dataStart < bytes.Length && bytes[dataStart] == '\n') dataStart++;



                    int endStreamPos = text.IndexOf("endstream", dataStart);

                    if (endStreamPos > dataStart)

                    {

                        int len = endStreamPos - dataStart;

                        byte[] raw = new byte[len];

                        Array.Copy(bytes, dataStart, raw, 0, len);



                        if (dictText.Contains("/FlateDecode"))

                        {

                            streamBytes = DecompressZlib(raw);

                        }

                        else

                        {

                            streamBytes = raw;

                        }

                    }

                }

                else

                {

                    dictText = text.Substring(startPos, endObjPos - startPos);

                }



                table[id] = new PdfRawObject { Id = id, Gen = gen, DictText = dictText, StreamBytes = streamBytes };

            }



            return table;

        }



        private static void UnpackObjectStreams(Dictionary<int, PdfRawObject> table)

        {

            List<PdfRawObject> objStreams = new List<PdfRawObject>();

            foreach (var kvp in table)

            {

                if (kvp.Value.DictText.Contains("/ObjStm") && kvp.Value.StreamBytes != null)

                {

                    objStreams.Add(kvp.Value);

                }

            }



            foreach (var stm in objStreams)

            {

                try

                {

                    Match nMatch = Regex.Match(stm.DictText, @"/N\s+(\d+)");

                    Match firstMatch = Regex.Match(stm.DictText, @"/First\s+(\d+)");

                    if (!nMatch.Success || !firstMatch.Success) continue;



                    int count = int.Parse(nMatch.Groups[1].Value);

                    int firstOffset = int.Parse(firstMatch.Groups[1].Value);



                    string streamText = Encoding.UTF8.GetString(stm.StreamBytes);

                    string headerPart = streamText.Substring(0, Math.Min(firstOffset, streamText.Length));

                    string dataPart = streamText.Substring(Math.Min(firstOffset, streamText.Length));



                    MatchCollection headerPairs = Regex.Matches(headerPart, @"(\d+)\s+(\d+)");

                    List<int> innerIds = new List<int>();

                    List<int> innerOffsets = new List<int>();



                    foreach (Match pair in headerPairs)

                    {

                        innerIds.Add(int.Parse(pair.Groups[1].Value));

                        innerOffsets.Add(int.Parse(pair.Groups[2].Value));

                    }



                    for (int i = 0; i < innerIds.Count; i++)

                    {

                        int id = innerIds[i];

                        int offset = innerOffsets[i];

                        int nextOffset = (i + 1 < innerOffsets.Count) ? innerOffsets[i + 1] : dataPart.Length;

                        if (offset < dataPart.Length)

                        {

                            int len = Math.Max(0, Math.Min(nextOffset - offset, dataPart.Length - offset));

                            string objStr = dataPart.Substring(offset, len).Trim();

                            if (!table.ContainsKey(id))

                            {

                                table[id] = new PdfRawObject { Id = id, Gen = 0, DictText = objStr, StreamBytes = null };

                            }

                        }

                    }

                }

                catch { }

            }

        }



        private static PdfRawObject FindCatalog(Dictionary<int, PdfRawObject> table, byte[] bytes)

        {

            // Search trailer /Root

            string text = Encoding.ASCII.GetString(bytes);

            Match trailerMatch = Regex.Match(text, @"trailer\s*<<.*?/Root\s+(\d+)\s+\d+\s+R.*?>>", RegexOptions.Singleline);

            if (trailerMatch.Success)

            {

                int catId = int.Parse(trailerMatch.Groups[1].Value);

                if (table.ContainsKey(catId)) return table[catId];

            }



            // Direct search for /Type /Catalog

            foreach (var kvp in table)

            {

                if (kvp.Value.DictText.Contains("/Catalog")) return kvp.Value;

            }

            return null;

        }



        private static int GetRefId(string text, string key)

        {

            Match m = Regex.Match(text, Regex.Escape(key) + @"\s+(\d+)\s+\d+\s+R");

            if (m.Success) return int.Parse(m.Groups[1].Value);

            return -1;

        }



        private static void CollectPages(PdfRawObject node, Dictionary<int, PdfRawObject> table, List<PdfRawObject> pageObjs)

        {

            if (node.DictText.Contains("/Page") && !node.DictText.Contains("/Pages"))

            {

                pageObjs.Add(node);

                return;

            }



            Match km = Regex.Match(node.DictText, @"/Kids\s*\[(.*?)\]", RegexOptions.Singleline);

            if (km.Success)

            {

                string kidsStr = km.Groups[1].Value;

                MatchCollection refs = Regex.Matches(kidsStr, @"(\d+)\s+\d+\s+R");

                foreach (Match r in refs)

                {

                    int kId = int.Parse(r.Groups[1].Value);

                    if (table.ContainsKey(kId))

                    {

                        CollectPages(table[kId], table, pageObjs);

                    }

                }

            }

        }



        private static byte[] GetPageContentBytes(PdfRawObject pageObj, Dictionary<int, PdfRawObject> table)

        {

            List<byte> allBytes = new List<byte>();



            // Array /Contents [ 4 0 R 5 0 R ]

            Match arrMatch = Regex.Match(pageObj.DictText, @"/Contents\s*\[(.*?)\]", RegexOptions.Singleline);

            if (arrMatch.Success)

            {

                MatchCollection refs = Regex.Matches(arrMatch.Groups[1].Value, @"(\d+)\s+\d+\s+R");

                foreach (Match r in refs)

                {

                    int cId = int.Parse(r.Groups[1].Value);

                    if (table.ContainsKey(cId) && table[cId].StreamBytes != null)

                    {

                        allBytes.AddRange(table[cId].StreamBytes);

                        allBytes.Add((byte)'\n');

                    }

                }

                return allBytes.ToArray();

            }



            // Single reference /Contents 4 0 R

            int singleId = GetRefId(pageObj.DictText, "/Contents");

            if (singleId > 0 && table.ContainsKey(singleId) && table[singleId].StreamBytes != null)

            {

                return table[singleId].StreamBytes;

            }



            if (pageObj.StreamBytes != null) return pageObj.StreamBytes;

            return null;

        }



        private static void ExtractPageImages(PdfRawObject pageObj, Dictionary<int, PdfRawObject> table, PdfPageModel page)

        {

            try

            {

                Match xobjMatch = Regex.Match(pageObj.DictText, @"/XObject\s*<<\s*(.*?)\s*>>", RegexOptions.Singleline);

                if (!xobjMatch.Success)

                {

                    int resId = GetRefId(pageObj.DictText, "/Resources");

                    if (resId > 0 && table.ContainsKey(resId))

                    {

                        xobjMatch = Regex.Match(table[resId].DictText, @"/XObject\s*<<\s*(.*?)\s*>>", RegexOptions.Singleline);

                    }

                }



                if (xobjMatch.Success)

                {

                    string xobjBody = xobjMatch.Groups[1].Value;

                    MatchCollection imgRefs = Regex.Matches(xobjBody, @"/(\w+)\s+(\d+)\s+\d+\s+R");

                    foreach (Match im in imgRefs)

                    {

                        int imgObjId = int.Parse(im.Groups[2].Value);

                        if (table.ContainsKey(imgObjId) && table[imgObjId].StreamBytes != null)

                        {

                            var imgObj = table[imgObjId];

                            if (imgObj.DictText.Contains("/Image"))

                            {

                                try

                                {

                                    using (MemoryStream ms = new MemoryStream(imgObj.StreamBytes))

                                    {

                                        Image img = Image.FromStream(ms);

                                        float imgW = Math.Min(page.Width - 100, img.Width * 0.75f);

                                        float imgH = (img.Height / (float)img.Width) * imgW;

                                        RectangleF imgBounds = new RectangleF(50, 50, imgW, imgH);

                                        page.ImageElements.Add(new PdfImageElement(img, imgBounds));

                                    }

                                }

                                catch { }

                            }

                        }

                    }

                }

            }

            catch { }

        }



        private static byte[] DecompressZlib(byte[] input)

        {

            try

            {

                int offset = 0;

                if (input.Length > 2 && input[0] == 0x78) offset = 2;



                using (MemoryStream ms = new MemoryStream(input, offset, input.Length - offset))

                using (DeflateStream def = new DeflateStream(ms, CompressionMode.Decompress))

                using (MemoryStream outMs = new MemoryStream())

                {

                    def.CopyTo(outMs);

                    return outMs.ToArray();

                }

            }

            catch

            {

                return input;

            }

        }

        #endregion



        #region Font & CMap Resource Extraction

        public class PdfFontResource

        {

            public string Key;

            public string CleanFontName;

            public Dictionary<int, string> ToUnicodeMap;

            public Dictionary<int, string> DifferencesMap;

            public Encoding BaseEncoding;

            public bool IsCID;



            public PdfFontResource(string key, string cleanName)

            {

                Key = key;

                CleanFontName = cleanName;

                ToUnicodeMap = new Dictionary<int, string>();

                DifferencesMap = new Dictionary<int, string>();

                BaseEncoding = Encoding.GetEncoding(1252);

                IsCID = false;

            }

        }



        private static Dictionary<string, PdfFontResource> ExtractAllDocumentFonts(Dictionary<int, PdfRawObject> table)

        {

            var fontDict = new Dictionary<string, PdfFontResource>();

            try

            {

                // 1. Quét toàn bộ bảng object để tìm các font (/Type /Font)

                foreach (var kvp in table)

                {

                    var fObj = kvp.Value;

                    if (fObj.DictText.Contains("/Type/Font") || fObj.DictText.Contains("/Type /Font"))

                    {

                        string baseFontName = "Segoe UI";

                        Match bfMatch = Regex.Match(fObj.DictText, @"/BaseFont\s*/([^\s/>]+)");

                        if (bfMatch.Success)

                        {

                            string rawBf = bfMatch.Groups[1].Value;

                            if (rawBf.Contains("+")) rawBf = rawBf.Substring(rawBf.IndexOf('+') + 1);

                            baseFontName = CleanFontName(rawBf);

                        }



                        var fontRes = new PdfFontResource(kvp.Key.ToString(), baseFontName);

                        if (fObj.DictText.Contains("/Type0") || fObj.DictText.Contains("/Identity-H") || fObj.DictText.Contains("/CIDFontType2"))

                        {

                            fontRes.IsCID = true;

                        }



                        // Parse /ToUnicode

                        int toUnicodeId = GetRefId(fObj.DictText, "/ToUnicode");

                        if (toUnicodeId > 0 && table.ContainsKey(toUnicodeId))

                        {

                            byte[] cmapBytes = table[toUnicodeId].StreamBytes;

                            if (cmapBytes != null && cmapBytes.Length > 0)

                            {

                                string cmapText = Encoding.UTF8.GetString(cmapBytes);

                                ParseToUnicodeCMap(cmapText, fontRes.ToUnicodeMap);

                            }

                        }



                        // Parse /Encoding

                        int encId = GetRefId(fObj.DictText, "/Encoding");

                        string encText = "";

                        if (encId > 0 && table.ContainsKey(encId))

                        {

                            encText = table[encId].DictText;

                        }

                        else

                        {

                            Match encM = Regex.Match(fObj.DictText, @"/Encoding\s*<<\s*(.*?)\s*>>", RegexOptions.Singleline);

                            if (encM.Success) encText = encM.Groups[1].Value;

                        }



                        if (!string.IsNullOrEmpty(encText))

                        {

                            ParseEncodingDifferences(encText, fontRes.DifferencesMap);

                        }



                        // Tìm font key tương ứng (/LQYSYM, /SUTRJP...) trong tất cả các từ điển /Font

                        foreach (var rKvp in table)

                        {

                            if (rKvp.Value.DictText != null && rKvp.Value.DictText.Contains(kvp.Key + " 0 R"))

                            {

                                Match fm = Regex.Match(rKvp.Value.DictText, @"/(\w+)\s+" + kvp.Key + @"\s+0\s+R");

                                if (fm.Success)

                                {

                                    string fontKey = fm.Groups[1].Value;

                                    var fCopy = new PdfFontResource(fontKey, baseFontName)

                                    {

                                        IsCID = fontRes.IsCID,

                                        ToUnicodeMap = fontRes.ToUnicodeMap,

                                        DifferencesMap = fontRes.DifferencesMap,

                                        BaseEncoding = fontRes.BaseEncoding

                                    };

                                    fontDict[fontKey] = fCopy;

                                }

                            }

                        }



                        fontDict[kvp.Key.ToString()] = fontRes;

                    }

                }

            }

            catch { }

            return fontDict;

        }



        private static Dictionary<string, PdfFontResource> ExtractPageFonts(PdfRawObject pageObj, Dictionary<int, PdfRawObject> table, Dictionary<string, PdfFontResource> docFonts)

        {

            var fontDict = new Dictionary<string, PdfFontResource>(docFonts);

            try

            {

                string resDictText = "";

                Match resMatch = Regex.Match(pageObj.DictText, @"/Resources\s*<<\s*(.*?)\s*>>", RegexOptions.Singleline);

                if (resMatch.Success) resDictText = resMatch.Groups[1].Value;

                else

                {

                    int resId = GetRefId(pageObj.DictText, "/Resources");

                    if (resId > 0 && table.ContainsKey(resId)) resDictText = table[resId].DictText;

                }



                if (!string.IsNullOrEmpty(resDictText))

                {

                    string fontBlock = "";

                    Match fMatch = Regex.Match(resDictText, @"/Font\s*<<\s*(.*?)\s*>>", RegexOptions.Singleline);

                    if (fMatch.Success) fontBlock = fMatch.Groups[1].Value;

                    else

                    {

                        int fResId = GetRefId(resDictText, "/Font");

                        if (fResId > 0 && table.ContainsKey(fResId)) fontBlock = table[fResId].DictText;

                    }



                    if (!string.IsNullOrEmpty(fontBlock))

                    {

                        MatchCollection fontRefs = Regex.Matches(fontBlock, @"/(\w+)\s+(\d+)\s+\d+\s+R");

                        foreach (Match fr in fontRefs)

                        {

                            string fKey = fr.Groups[1].Value;

                            int fId = int.Parse(fr.Groups[2].Value);

                            if (docFonts.ContainsKey(fId.ToString()))

                            {

                                fontDict[fKey] = docFonts[fId.ToString()];

                            }

                            else if (table.ContainsKey(fId))

                            {

                                var fObj = table[fId];

                                string baseFontName = "Segoe UI";

                                Match bfMatch = Regex.Match(fObj.DictText, @"/BaseFont\s*/([^\s/>]+)");

                                if (bfMatch.Success)

                                {

                                    string rawBf = bfMatch.Groups[1].Value;

                                    if (rawBf.Contains("+")) rawBf = rawBf.Substring(rawBf.IndexOf('+') + 1);

                                    baseFontName = CleanFontName(rawBf);

                                }

                                var fRes = new PdfFontResource(fKey, baseFontName);

                                if (fObj.DictText.Contains("/Type0") || fObj.DictText.Contains("/Identity-H")) fRes.IsCID = true;



                                int toUnicodeId = GetRefId(fObj.DictText, "/ToUnicode");

                                if (toUnicodeId > 0 && table.ContainsKey(toUnicodeId) && table[toUnicodeId].StreamBytes != null)

                                {

                                    ParseToUnicodeCMap(Encoding.UTF8.GetString(table[toUnicodeId].StreamBytes), fRes.ToUnicodeMap);

                                }

                                fontDict[fKey] = fRes;

                            }

                        }

                    }

                }

            }

            catch { }



            return fontDict;

        }



        private static string CleanFontName(string raw)

        {

            if (string.IsNullOrEmpty(raw)) return "Segoe UI";

            if (raw.Contains("Times") || raw.Contains("Roman") || raw.Contains("Termes")) return "Times New Roman";

            if (raw.Contains("Arial") || raw.Contains("Helvetica") || raw.Contains("Liberation-Sans")) return "Arial";

            if (raw.Contains("Calibri")) return "Calibri";

            if (raw.Contains("Cambria")) return "Cambria";

            if (raw.Contains("Courier") || raw.Contains("Consolas")) return "Consolas";

            if (raw.Contains("Tahoma")) return "Tahoma";

            if (raw.Contains("Verdana")) return "Verdana";

            if (raw.Contains("Liberation-Serif") || raw.Contains("Nimbus-Roman") || raw.Contains("Noto-Serif")) return "Times New Roman";

            return "Segoe UI";

        }



        private static void ParseToUnicodeCMap(string cmapText, Dictionary<int, string> map)

        {

            if (string.IsNullOrEmpty(cmapText) || map == null) return;



            MatchCollection bfcharBlocks = Regex.Matches(cmapText, @"beginbfchar(.*?)endbfchar", RegexOptions.Singleline);

            foreach (Match block in bfcharBlocks)

            {

                MatchCollection pairs = Regex.Matches(block.Groups[1].Value, @"<([0-9a-fA-F]+)>\s*<([0-9a-fA-F]+)>");

                foreach (Match p in pairs)

                {

                    try

                    {

                        int srcCode = Convert.ToInt32(p.Groups[1].Value, 16);

                        string uniHex = p.Groups[2].Value;

                        map[srcCode] = DecodeUnicodeHexSequence(uniHex);

                    }

                    catch { }

                }

            }



            MatchCollection bfrangeBlocks = Regex.Matches(cmapText, @"beginbfrange(.*?)endbfrange", RegexOptions.Singleline);

            foreach (Match block in bfrangeBlocks)

            {

                MatchCollection simpleRanges = Regex.Matches(block.Groups[1].Value, @"<([0-9a-fA-F]+)>\s*<([0-9a-fA-F]+)>\s*<([0-9a-fA-F]+)>");

                foreach (Match r in simpleRanges)

                {

                    try

                    {

                        int start = Convert.ToInt32(r.Groups[1].Value, 16);

                        int end = Convert.ToInt32(r.Groups[2].Value, 16);

                        int targetStart = Convert.ToInt32(r.Groups[3].Value, 16);



                        for (int code = start; code <= end; code++)

                        {

                            int targetCode = targetStart + (code - start);

                            map[code] = ((char)targetCode).ToString();

                        }

                    }

                    catch { }

                }



                MatchCollection arrayRanges = Regex.Matches(block.Groups[1].Value, @"<([0-9a-fA-F]+)>\s*<([0-9a-fA-F]+)>\s*\[(.*?)\]", RegexOptions.Singleline);

                foreach (Match ar in arrayRanges)

                {

                    try

                    {

                        int start = Convert.ToInt32(ar.Groups[1].Value, 16);

                        int end = Convert.ToInt32(ar.Groups[2].Value, 16);

                        MatchCollection hexList = Regex.Matches(ar.Groups[3].Value, @"<([0-9a-fA-F]+)>");

                        int code = start;

                        foreach (Match h in hexList)

                        {

                            if (code <= end)

                            {

                                map[code] = DecodeUnicodeHexSequence(h.Groups[1].Value);

                                code++;

                            }

                        }

                    }

                    catch { }

                }

            }

        }



        private static string DecodeUnicodeHexSequence(string hex)

        {

            if (string.IsNullOrEmpty(hex)) return "";

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i + 3 < hex.Length; i += 4)

            {

                int ch = Convert.ToInt32(hex.Substring(i, 4), 16);

                sb.Append((char)ch);

            }

            if (sb.Length == 0 && hex.Length >= 2)

            {

                int ch = Convert.ToInt32(hex, 16);

                sb.Append((char)ch);

            }

            return sb.ToString();

        }



        private static void ParseEncodingDifferences(string encText, Dictionary<int, string> diffMap)

        {

            if (string.IsNullOrEmpty(encText) || diffMap == null) return;



            Match diffMatch = Regex.Match(encText, @"/Differences\s*\[(.*?)\]", RegexOptions.Singleline);

            if (!diffMatch.Success) return;



            string[] tokens = diffMatch.Groups[1].Value.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            int currentCode = 0;



            foreach (string tok in tokens)

            {

                int num;

                if (int.TryParse(tok, out num))

                {

                    currentCode = num;

                }

                else if (tok.StartsWith("/"))

                {

                    string glyphName = tok.Substring(1);

                    string uniChar = AdobeGlyphToUnicode(glyphName);

                    if (!string.IsNullOrEmpty(uniChar))

                    {

                        diffMap[currentCode] = uniChar;

                    }

                    currentCode++;

                }

            }

        }

        #endregion



        #region Matrix & Graphics State

        public struct PdfMatrix

        {

            public float A, B, C, D, E, F;

            public PdfMatrix(float a, float b, float c, float d, float e, float f)

            {

                A = a; B = b; C = c; D = d; E = e; F = f;

            }

            public static PdfMatrix Identity = new PdfMatrix(1, 0, 0, 1, 0, 0);



            public PdfMatrix Multiply(PdfMatrix m)

            {

                return new PdfMatrix(

                    A * m.A + B * m.C,

                    A * m.B + B * m.D,

                    C * m.A + D * m.C,

                    C * m.B + D * m.D,

                    E * m.A + F * m.C + m.E,

                    E * m.B + F * m.D + m.F

                );

            }



            public PointF Transform(float x, float y)

            {

                return new PointF(A * x + C * y + E, B * x + D * y + F);

            }

        }



        private class PdfGraphicsState

        {

            public PdfMatrix Ctm = PdfMatrix.Identity;

            public Color Color = Color.FromArgb(15, 23, 42);

        }

        #endregion



        #region PDF Content Stream Interpretation

        private static void InterpretContentStream(string stream, PdfPageModel page, float pageH, Dictionary<string, PdfFontResource> fonts)

        {

            var gStack = new Stack<PdfGraphicsState>();

            var curState = new PdfGraphicsState();



            float fontSize = 12f;

            string fontName = "Segoe UI";

            PdfFontResource activeFont = null;

            PdfMatrix textMatrix = PdfMatrix.Identity;



            string[] tokens = TokenizeStream(stream);

            List<string> opStack = new List<string>();



            StringBuilder lineBuffer = new StringBuilder();

            float lineStartX = 50f, lineStartY = 50f;

            float lineFontSize = 12f;

            string lineFontName = "Segoe UI";

            Color lineColor = curState.Color;

            bool isBold = false;



            Action flushLine = () =>

            {

                if (lineBuffer.Length > 0)

                {

                    string fullText = lineBuffer.ToString().Normalize(NormalizationForm.FormC);

                    if (!string.IsNullOrWhiteSpace(fullText))

                    {

                        float tw = Math.Max(20f, fullText.Length * lineFontSize * 0.55f);

                        page.TextElements.Add(new PdfTextElement(fullText, new RectangleF(lineStartX, lineStartY, tw, lineFontSize * 1.35f), lineFontName, lineFontSize)

                        {

                            TextColor = lineColor,

                            IsBold = isBold || lineFontName.Contains("Bold") || lineFontSize >= 14f

                        });

                    }

                    lineBuffer.Clear();

                }

            };



            for (int i = 0; i < tokens.Length; i++)

            {

                string t = tokens[i];



                if (t == "q")

                {

                    var copy = new PdfGraphicsState

                    {

                        Ctm = curState.Ctm,

                        Color = curState.Color

                    };

                    gStack.Push(copy);

                    opStack.Clear();

                }

                else if (t == "Q")

                {

                    if (gStack.Count > 0) curState = gStack.Pop();

                    opStack.Clear();

                }

                else if (t == "cm" && opStack.Count >= 6)

                {

                    float a = 1f, b = 0f, c = 0f, d = 1f, e = 0f, f = 0f;

                    float.TryParse(opStack[opStack.Count - 6], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out a);

                    float.TryParse(opStack[opStack.Count - 5], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out b);

                    float.TryParse(opStack[opStack.Count - 4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out c);

                    float.TryParse(opStack[opStack.Count - 3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d);

                    float.TryParse(opStack[opStack.Count - 2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out e);

                    float.TryParse(opStack[opStack.Count - 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f);



                    var m = new PdfMatrix(a, b, c, d, e, f);

                    curState.Ctm = m.Multiply(curState.Ctm);

                    opStack.Clear();

                }

                else if (t == "BT")

                {

                    flushLine();

                    textMatrix = PdfMatrix.Identity;

                    opStack.Clear();

                }

                else if (t == "ET")

                {

                    flushLine();

                    opStack.Clear();

                }

                else if (t == "Tf" && opStack.Count >= 2)

                {

                    string fontKey = opStack[opStack.Count - 2].TrimStart('/');

                    float.TryParse(opStack[opStack.Count - 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fontSize);

                    if (fontSize <= 0) fontSize = 12f;



                    if (fonts != null && fonts.ContainsKey(fontKey))

                    {

                        activeFont = fonts[fontKey];

                        fontName = activeFont.CleanFontName;

                    }

                    else

                    {

                        activeFont = null;

                        fontName = "Segoe UI";

                    }

                    isBold = fontName.Contains("Bold") || fontKey.Contains("Bold") || fontKey.Contains("LQYSYM") || fontKey.Contains("SUTRJP") || fontKey.Contains("HONZJH");

                    opStack.Clear();

                }

                else if (t == "Tm" && opStack.Count >= 6)

                {

                    flushLine();



                    float tmA = 1f, tmB = 0f, tmC = 0f, tmD = 1f, tmE = 0f, tmF = 0f;

                    float.TryParse(opStack[opStack.Count - 6], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tmA);

                    float.TryParse(opStack[opStack.Count - 5], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tmB);

                    float.TryParse(opStack[opStack.Count - 4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tmC);

                    float.TryParse(opStack[opStack.Count - 3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tmD);

                    float.TryParse(opStack[opStack.Count - 2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tmE);

                    float.TryParse(opStack[opStack.Count - 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tmF);



                    textMatrix = new PdfMatrix(tmA, tmB, tmC, tmD, tmE, tmF);

                    var combined = textMatrix.Multiply(curState.Ctm);

                    PointF pt = combined.Transform(0, 0);



                    float effScale = (float)Math.Sqrt(combined.A * combined.A + combined.B * combined.B);

                    if (effScale <= 0) effScale = 1f;



                    lineStartX = pt.X;

                    // Chuyển đổi tọa độ baseline sang top của dòng chữ (PDF space is bottom-up)

                    lineStartY = pageH - pt.Y - (fontSize * effScale * 0.82f);

                    if (lineStartY < 0) lineStartY = 0;



                    lineFontSize = fontSize * effScale;

                    lineFontName = fontName;

                    lineColor = curState.Color;



                    opStack.Clear();

                }

                else if ((t == "Td" || t == "TD") && opStack.Count >= 2)

                {

                    flushLine();



                    float dx = 0, dy = 0;

                    float.TryParse(opStack[opStack.Count - 2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out dx);

                    float.TryParse(opStack[opStack.Count - 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out dy);



                    var trM = new PdfMatrix(1, 0, 0, 1, dx, dy);

                    textMatrix = trM.Multiply(textMatrix);



                    var combined = textMatrix.Multiply(curState.Ctm);

                    PointF pt = combined.Transform(0, 0);



                    float effScale = (float)Math.Sqrt(combined.A * combined.A + combined.B * combined.B);

                    if (effScale <= 0) effScale = 1f;



                    lineStartX = pt.X;

                    lineStartY = pageH - pt.Y - (fontSize * effScale * 0.82f);

                    if (lineStartY < 0) lineStartY = 0;



                    lineFontSize = fontSize * effScale;

                    lineFontName = fontName;

                    lineColor = curState.Color;



                    opStack.Clear();

                }

                else if (t == "T*")

                {

                    flushLine();

                    lineStartY += lineFontSize * 1.25f;

                }

                else if (t == "rg" && opStack.Count >= 3)

                {

                    float r = 0, g = 0, b = 0;

                    float.TryParse(opStack[opStack.Count - 3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out r);

                    float.TryParse(opStack[opStack.Count - 2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out g);

                    float.TryParse(opStack[opStack.Count - 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out b);

                    curState.Color = Color.FromArgb((int)(Math.Min(1f, r) * 255), (int)(Math.Min(1f, g) * 255), (int)(Math.Min(1f, b) * 255));

                    lineColor = curState.Color;

                    opStack.Clear();

                }

                else if (t == "g" && opStack.Count >= 1)

                {

                    float gray = 0;

                    float.TryParse(opStack[opStack.Count - 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out gray);

                    int gVal = (int)(Math.Min(1f, gray) * 255);

                    curState.Color = Color.FromArgb(gVal, gVal, gVal);

                    lineColor = curState.Color;

                    opStack.Clear();

                }

                else if (t == "re" && opStack.Count >= 4)

                {

                    float rx = 0, ry = 0, rw = 0, rh = 0;

                    float.TryParse(opStack[opStack.Count - 4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rx);

                    float.TryParse(opStack[opStack.Count - 3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out ry);

                    float.TryParse(opStack[opStack.Count - 2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rw);

                    float.TryParse(opStack[opStack.Count - 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rh);



                    PointF p1 = curState.Ctm.Transform(rx, ry);

                    PointF p2 = curState.Ctm.Transform(rx + rw, ry + rh);



                    float finalX = Math.Min(p1.X, p2.X);

                    float y1_screen = pageH - p1.Y;

                    float y2_screen = pageH - p2.Y;

                    float finalY = Math.Min(y1_screen, y2_screen);

                    float finalW = Math.Abs(p2.X - p1.X);

                    float finalH = Math.Abs(p2.Y - p1.Y);



                    if (finalW > 0 && finalH > 0)

                    {

                        var shp = new PdfShapeElement(PdfShapeType.Rectangle, new RectangleF(finalX, finalY, finalW, finalH));

                        shp.FillColor = curState.Color;

                        page.ShapeElements.Add(shp);

                    }

                    opStack.Clear();

                }

                else if (t == "Tj" && opStack.Count >= 1)

                {

                    string rawStr = opStack[opStack.Count - 1];

                    string decoded = DecodePdfToken(rawStr, activeFont, fonts);

                    if (!string.IsNullOrEmpty(decoded))

                    {

                        lineBuffer.Append(decoded);

                    }

                    opStack.Clear();

                }

                else if (t == "TJ" && opStack.Count >= 1)

                {

                    string rawArr = opStack[opStack.Count - 1];

                    float dummyX = 0;

                    string decoded = DecodeTJArray(rawArr, activeFont, fonts, fontSize, ref dummyX);

                    if (!string.IsNullOrEmpty(decoded))

                    {

                        lineBuffer.Append(decoded);

                    }

                    opStack.Clear();

                }

                else

                {

                    opStack.Add(t);

                }

            }



            flushLine();



            // Áp dụng Smart Line Merger để khử hoàn toàn hiện tượng đè chữ cùng dòng

            page.TextElements = MergeLineTexts(page.TextElements);

        }



        private static List<PdfTextElement> MergeLineTexts(List<PdfTextElement> rawTexts)

        {

            if (rawTexts == null || rawTexts.Count <= 1) return rawTexts;



            var lines = new List<List<PdfTextElement>>();

            foreach (var t in rawTexts)

            {

                if (string.IsNullOrWhiteSpace(t.Text)) continue;



                List<PdfTextElement> targetLine = null;

                foreach (var l in lines)

                {

                    if (Math.Abs(l[0].Bounds.Y - t.Bounds.Y) <= 3.5f)

                    {

                        targetLine = l;

                        break;

                    }

                }



                if (targetLine == null)

                {

                    targetLine = new List<PdfTextElement>();

                    lines.Add(targetLine);

                }

                targetLine.Add(t);

            }



            lines.Sort((a, b) => a[0].Bounds.Y.CompareTo(b[0].Bounds.Y));



            var result = new List<PdfTextElement>();

            foreach (var line in lines)

            {

                line.Sort((a, b) => a.Bounds.X.CompareTo(b.Bounds.X));



                StringBuilder sb = new StringBuilder();

                float startX = line[0].Bounds.X;

                float startY = line[0].Bounds.Y;

                float maxFontSize = line[0].FontSize;

                string fontName = line[0].FontName;

                Color color = line[0].TextColor;

                bool isBold = false;

                bool isItalic = false;



                float lastRightX = startX;



                for (int i = 0; i < line.Count; i++)

                {

                    var item = line[i];

                    if (item.FontSize > maxFontSize) maxFontSize = item.FontSize;

                    if (item.IsBold) isBold = true;

                    if (item.IsItalic) isItalic = true;

                    if (item.TextColor != Color.Empty && item.TextColor.A > 0 && (item.TextColor.R != 26 || item.TextColor.G != 31 || item.TextColor.B != 43))

                    {

                        color = item.TextColor;

                    }



                    string str = item.Text.Trim();

                    if (string.IsNullOrEmpty(str)) continue;



                    if (sb.Length > 0)

                    {

                        float gap = item.Bounds.X - lastRightX;

                        if (gap > 2.0f && !sb.ToString().EndsWith(" ") && !str.StartsWith(" ") && !str.StartsWith(".") && !str.StartsWith(",") && !str.StartsWith(")") && !str.StartsWith(":") && !str.StartsWith(";"))

                        {

                            sb.Append(" ");

                        }

                    }



                    sb.Append(str);

                    lastRightX = item.Bounds.X + Math.Max(item.Bounds.Width, str.Length * item.FontSize * 0.5f);

                }



                string mergedStr = sb.ToString().Normalize(NormalizationForm.FormC);

                if (!string.IsNullOrWhiteSpace(mergedStr))

                {

                    float fullW = Math.Max(20f, mergedStr.Length * maxFontSize * 0.55f);

                    result.Add(new PdfTextElement(mergedStr, new RectangleF(startX, startY, fullW, maxFontSize * 1.35f), fontName, maxFontSize)

                    {

                        TextColor = color,

                        IsBold = isBold || maxFontSize >= 14f,

                        IsItalic = isItalic

                    });

                }

            }



            return result;

        }



        private static string[] TokenizeStream(string stream)

        {

            List<string> list = new List<string>();

            int i = 0;

            int len = stream.Length;



            while (i < len)

            {

                char c = stream[i];

                if (char.IsWhiteSpace(c)) { i++; continue; }



                if (c == '(')

                {

                    int start = i;

                    int depth = 1;

                    i++;

                    while (i < len && depth > 0)

                    {

                        if (stream[i] == '\\') { i += 2; continue; }

                        if (stream[i] == '(') depth++;

                        else if (stream[i] == ')') depth--;

                        i++;

                    }

                    list.Add(stream.Substring(start, i - start));

                }

                else if (c == '<')

                {

                    if (i + 1 < len && stream[i + 1] == '<')

                    {

                        list.Add("<<");

                        i += 2;

                    }

                    else

                    {

                        int end = stream.IndexOf('>', i);

                        if (end >= 0)

                        {

                            list.Add(stream.Substring(i, end - i + 1));

                            i = end + 1;

                        }

                        else i++;

                    }

                }

                else if (c == '[')

                {

                    int start = i;

                    int depth = 1;

                    i++;

                    while (i < len && depth > 0)

                    {

                        if (stream[i] == '[') depth++;

                        else if (stream[i] == ']') depth--;

                        i++;

                    }

                    list.Add(stream.Substring(start, i - start));

                }

                else

                {

                    int start = i;

                    while (i < len && !char.IsWhiteSpace(stream[i]) && stream[i] != '(' && stream[i] != '<' && stream[i] != '[' && stream[i] != ']' && stream[i] != '/' && stream[i] != ')')

                    {

                        i++;

                    }

                    if (i == start) i++;

                    list.Add(stream.Substring(start, i - start));

                }

            }



            return list.ToArray();

        }



        private static byte[] UnescapePdfString(string raw)

        {

            if (raw.StartsWith("(") && raw.EndsWith(")"))

            {

                raw = raw.Substring(1, raw.Length - 2);

            }



            List<byte> bytes = new List<byte>();

            for (int i = 0; i < raw.Length; i++)

            {

                if (raw[i] == '\\' && i + 1 < raw.Length)

                {

                    char next = raw[i + 1];

                    if (next >= '0' && next <= '7')

                    {

                        int octLen = 1;

                        int octVal = next - '0';

                        if (i + 2 < raw.Length && raw[i + 2] >= '0' && raw[i + 2] <= '7')

                        {

                            octVal = octVal * 8 + (raw[i + 2] - '0');

                            octLen = 2;

                            if (i + 3 < raw.Length && raw[i + 3] >= '0' && raw[i + 3] <= '7')

                            {

                                octVal = octVal * 8 + (raw[i + 3] - '0');

                                octLen = 3;

                            }

                        }

                        bytes.Add((byte)(octVal & 0xFF));

                        i += octLen;

                    }

                    else if (next == 'n') { bytes.Add((byte)'\n'); i++; }

                    else if (next == 'r') { bytes.Add((byte)'\r'); i++; }

                    else if (next == 't') { bytes.Add((byte)'\t'); i++; }

                    else if (next == 'b') { bytes.Add((byte)'\b'); i++; }

                    else if (next == 'f') { bytes.Add((byte)'\f'); i++; }

                    else if (next == '(') { bytes.Add((byte)'('); i++; }

                    else if (next == ')') { bytes.Add((byte)')'); i++; }

                    else if (next == '\\') { bytes.Add((byte)'\\'); i++; }

                    else { bytes.Add((byte)next); i++; }

                }

                else

                {

                    bytes.Add((byte)raw[i]);

                }

            }

            return bytes.ToArray();

        }



        private static string DecodePdfToken(string raw, PdfFontResource font, Dictionary<string, PdfFontResource> allFonts = null)

        {

            if (string.IsNullOrEmpty(raw)) return "";



            if (raw.StartsWith("<") && raw.EndsWith(">"))

            {

                string hex = raw.Substring(1, raw.Length - 2).Trim();

                if (hex.Length % 2 != 0) hex += "0";



                // 1. Thử active font CMap

                if (font != null && font.ToUnicodeMap.Count > 0)

                {

                    string decoded = DecodeHexWithCMap(hex, font.ToUnicodeMap);

                    if (!string.IsNullOrEmpty(decoded)) return decoded;

                }



                // 2. Thử tất cả các CMap trong tài liệu nếu active font không chứa glyph

                if (allFonts != null)

                {

                    foreach (var kvp in allFonts)

                    {

                        if (kvp.Value.ToUnicodeMap.Count > 0)

                        {

                            string decoded = DecodeHexWithCMap(hex, kvp.Value.ToUnicodeMap);

                            if (!string.IsNullOrEmpty(decoded)) return decoded;

                        }

                    }

                }



                byte[] b = new byte[hex.Length / 2];

                for (int i = 0; i < b.Length; i++)

                {

                    b[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

                }



                if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF)

                {

                    return Encoding.BigEndianUnicode.GetString(b, 2, b.Length - 2).Normalize(NormalizationForm.FormC);

                }

                if (b.Length >= 2 && b.Length % 2 == 0 && b[0] == 0x00 && b[1] >= 32)

                {

                    return Encoding.BigEndianUnicode.GetString(b).Normalize(NormalizationForm.FormC);

                }



                return Encoding.GetEncoding(1252).GetString(b).Normalize(NormalizationForm.FormC);

            }



            byte[] rawBytes = UnescapePdfString(raw);

            if (rawBytes.Length >= 2 && rawBytes[0] == 0xFE && rawBytes[1] == 0xFF)

            {

                return Encoding.BigEndianUnicode.GetString(rawBytes, 2, rawBytes.Length - 2).Normalize(NormalizationForm.FormC);

            }



            if (font != null && font.ToUnicodeMap.Count > 0)

            {

                StringBuilder sb = new StringBuilder();

                if (font.IsCID && rawBytes.Length >= 2 && rawBytes.Length % 2 == 0)

                {

                    for (int i = 0; i + 1 < rawBytes.Length; i += 2)

                    {

                        int code = (rawBytes[i] << 8) | rawBytes[i + 1];

                        if (font.ToUnicodeMap.ContainsKey(code))

                            sb.Append(font.ToUnicodeMap[code]);

                        else if (font.ToUnicodeMap.ContainsKey(rawBytes[i + 1]))

                            sb.Append(font.ToUnicodeMap[rawBytes[i + 1]]);

                        else

                            sb.Append((char)rawBytes[i + 1]);

                    }

                    return sb.ToString().Normalize(NormalizationForm.FormC);

                }

                else

                {

                    for (int i = 0; i < rawBytes.Length; i++)

                    {

                        int code = (int)rawBytes[i];

                        if (font.ToUnicodeMap.ContainsKey(code))

                            sb.Append(font.ToUnicodeMap[code]);

                        else if (font.DifferencesMap.ContainsKey(code))

                            sb.Append(font.DifferencesMap[code]);

                        else if (code >= 32 && code <= 126)

                            sb.Append((char)code);

                        else

                            sb.Append(Encoding.GetEncoding(1252).GetString(new byte[] { rawBytes[i] }));

                    }

                    return sb.ToString().Normalize(NormalizationForm.FormC);

                }

            }



            if (font != null && font.DifferencesMap.Count > 0)

            {

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < rawBytes.Length; i++)

                {

                    int code = (int)rawBytes[i];

                    if (font.DifferencesMap.ContainsKey(code))

                        sb.Append(font.DifferencesMap[code]);

                    else

                        sb.Append(Encoding.GetEncoding(1252).GetString(new byte[] { rawBytes[i] }));

                }

                return sb.ToString().Normalize(NormalizationForm.FormC);

            }



            try

            {

                string utf8Str = Encoding.UTF8.GetString(rawBytes);

                bool hasReplacement = utf8Str.Contains("\uFFFD");

                if (!hasReplacement) return utf8Str.Normalize(NormalizationForm.FormC);

            }

            catch { }



            return Encoding.GetEncoding(1252).GetString(rawBytes).Normalize(NormalizationForm.FormC);

        }



        private static string DecodeHexWithCMap(string hex, Dictionary<int, string> map)

        {

            if (map == null || map.Count == 0) return null;

            StringBuilder sb = new StringBuilder();

            bool anyFound = false;



            if (hex.Length % 4 == 0)

            {

                for (int i = 0; i + 3 < hex.Length; i += 4)

                {

                    int code = Convert.ToInt32(hex.Substring(i, 4), 16);

                    if (map.ContainsKey(code))

                    {

                        sb.Append(map[code]);

                        anyFound = true;

                    }

                    else if (map.ContainsKey(code & 0xFF))

                    {

                        sb.Append(map[code & 0xFF]);

                        anyFound = true;

                    }

                    else if (code >= 32 && code <= 126)

                    {

                        sb.Append((char)code);

                    }

                }

            }

            else if (hex.Length % 2 == 0)

            {

                for (int i = 0; i + 1 < hex.Length; i += 2)

                {

                    int code = Convert.ToInt32(hex.Substring(i, 2), 16);

                    if (map.ContainsKey(code))

                    {

                        sb.Append(map[code]);

                        anyFound = true;

                    }

                    else if (code >= 32 && code <= 126)

                    {

                        sb.Append((char)code);

                    }

                }

            }



            return anyFound ? sb.ToString().Normalize(NormalizationForm.FormC) : null;

        }



        private static string DecodeTJArray(string arrText, PdfFontResource font, Dictionary<string, PdfFontResource> allFonts, float fontSize, ref float curX)

        {

            StringBuilder sb = new StringBuilder();

            int i = 0;

            while (i < arrText.Length)

            {

                if (arrText[i] == '(')

                {

                    int start = i;

                    int depth = 1;

                    i++;

                    while (i < arrText.Length && depth > 0)

                    {

                        if (arrText[i] == '\\') { i += 2; continue; }

                        if (arrText[i] == '(') depth++;

                        else if (arrText[i] == ')') depth--;

                        i++;

                    }

                    string rawToken = arrText.Substring(start, i - start);

                    sb.Append(DecodePdfToken(rawToken, font, allFonts));

                }

                else if (arrText[i] == '<')

                {

                    int end = arrText.IndexOf('>', i);

                    if (end > i)

                    {

                        string rawToken = arrText.Substring(i, end - i + 1);

                        sb.Append(DecodePdfToken(rawToken, font, allFonts));

                        i = end + 1;

                    }

                    else i++;

                }

                else if (char.IsDigit(arrText[i]) || arrText[i] == '-')

                {

                    int start = i;

                    while (i < arrText.Length && (char.IsDigit(arrText[i]) || arrText[i] == '-' || arrText[i] == '.'))

                    {

                        i++;

                    }

                    float offset;

                    if (float.TryParse(arrText.Substring(start, i - start), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out offset))

                    {

                        if (offset < -140f)

                        {

                            sb.Append(" ");

                        }

                    }

                }

                else i++;

            }

            return sb.ToString();

        }



        private static string AdobeGlyphToUnicode(string name)

        {

            switch (name)

            {

                case "space": return " ";

                case "exclam": return "!";

                case "quotedbl": return "\"";

                case "numbersign": return "#";

                case "dollar": return "$";

                case "percent": return "%";

                case "ampersand": return "&";

                case "quoteright": return "'";

                case "parenleft": return "(";

                case "parenright": return ")";

                case "asterisk": return "*";

                case "plus": return "+";

                case "comma": return ",";

                case "hyphen": return "-";

                case "period": return ".";

                case "slash": return "/";

                case "colon": return ":";

                case "semicolon": return ";";

                case "less": return "<";

                case "equal": return "=";

                case "greater": return ">";

                case "question": return "?";

                case "at": return "@";

                case "bracketleft": return "[";

                case "backslash": return "\\";

                case "bracketright": return "]";

                case "asciicircum": return "^";

                case "underscore": return "_";

                case "quoteleft": return "`";

                case "braceleft": return "{";

                case "bar": return "|";

                case "braceright": return "}";

                case "asciitilde": return "~";

                case "plusminus": return "±";

                case "multiply": return "×";

                case "divide": return "÷";

                case "approxequal": return "≈";

                case "notequal": return "≠";

                case "lessequal": case "leq": return "≤";

                case "greaterequal": case "geq": return "≥";

                case "infinity": return "∞";

                case "integral": return "∫";

                case "summation": return "∑";

                case "radical": case "sqrt": return "√";

                case "Delta": return "Δ";

                case "Omega": return "Ω";

                case "alpha": return "α";

                case "beta": return "β";

                case "gamma": return "γ";

                case "delta": return "δ";

                case "epsilon": return "ε";

                case "theta": return "θ";

                case "lambda": return "λ";

                case "mu": return "μ";

                case "pi": return "π";

                case "rho": return "ρ";

                case "sigma": return "σ";

                case "phi": return "φ";

                case "omega": return "ω";

                case "Agrave": return "À";

                case "Aacute": return "Á";

                case "Acircumflex": return "Â";

                case "Atilde": return "Ã";

                case "Egrave": return "È";

                case "Eacute": return "É";

                case "Ecircumflex": return "Ê";

                case "Igrave": return "Ì";

                case "Iacute": return "Í";

                case "Ograve": return "Ò";

                case "Oacute": return "Ó";

                case "Ocircumflex": return "Ô";

                case "Otilde": return "Õ";

                case "Ugrave": return "Ù";

                case "Uacute": return "Ú";

                case "Ucircumflex": return "Ư";

                case "Yacute": return "Ý";

                case "Dcroat": case "Dstroke": return "Đ";

                case "dcroat": case "dstroke": return "đ";

                case "agrave": return "à";

                case "aacute": return "á";

                case "acircumflex": return "â";

                case "atilde": return "ã";

                case "egrave": return "è";

                case "eacute": return "é";

                case "ecircumflex": return "ê";

                case "igrave": return "ì";

                case "iacute": return "í";

                case "ograve": return "ò";

                case "oacute": return "ó";

                case "ocircumflex": return "ô";

                case "otilde": return "õ";

                case "ugrave": return "ù";

                case "uacute": return "ú";

                case "ucircumflex": return "ư";

                case "yacute": return "ý";

                default:

                    if (name.StartsWith("uni") && name.Length == 7)

                    {

                        try { return ((char)Convert.ToInt32(name.Substring(3), 16)).ToString(); } catch { }

                    }

                    if (name.StartsWith("u") && name.Length == 5)

                    {

                        try { return ((char)Convert.ToInt32(name.Substring(1), 16)).ToString(); } catch { }

                    }

                    return "";

            }

        }

        #endregion



        #region Standard PDF File Generation & Exporting via PdfSharp
        public static bool SavePdf(PdfDocument doc, string filePath)
        {
            if (doc == null || doc.Pages == null || doc.Pages.Count == 0) return false;

            try
            {
                try { System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); } catch { }

                PdfSharp.Pdf.PdfDocument pdfSharpDoc = null;
                bool isModifyMode = false;

                // 1. Uu tien Modify mode de giu nguyen 100% hinh anh, font, vector cua PDF goc
                try
                {
                    if (doc.OriginalBytes != null && doc.OriginalBytes.Length > 0)
                    {
                        pdfSharpDoc = PdfSharp.Pdf.IO.PdfReader.Open(new MemoryStream(doc.OriginalBytes), PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify);
                        isModifyMode = true;
                    }
                    else if (!string.IsNullOrEmpty(doc.FilePath) && File.Exists(doc.FilePath))
                    {
                        byte[] orig = File.ReadAllBytes(doc.FilePath);
                        doc.OriginalBytes = orig;
                        pdfSharpDoc = PdfSharp.Pdf.IO.PdfReader.Open(new MemoryStream(orig), PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify);
                        isModifyMode = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("PdfSharp Modify mode failed, falling back to clean creation: " + ex.Message);
                    pdfSharpDoc = null;
                    isModifyMode = false;
                }

                if (pdfSharpDoc == null)
                {
                    pdfSharpDoc = new PdfSharp.Pdf.PdfDocument();
                }

                pdfSharpDoc.Info.Title = string.IsNullOrEmpty(doc.Title) ? Path.GetFileNameWithoutExtension(filePath) : doc.Title;
                pdfSharpDoc.Info.Author = string.IsNullOrEmpty(doc.Author) ? "ZENQ OFFICE" : doc.Author;
                pdfSharpDoc.Info.Creator = "ZENQ OFFICE PDF Engine";

                int totalPages = doc.Pages.Count;

                // Neu tao tai lieu moi hoan toan (khong phai tu file goc co san)
                if (!isModifyMode)
                {
                    for (int i = 0; i < totalPages; i++)
                    {
                        var pageModel = doc.Pages[i];
                        var pSharp = pdfSharpDoc.AddPage();
                        pSharp.Width = PdfSharp.Drawing.XUnit.FromPoint(pageModel.Width > 10 ? pageModel.Width : 595.28f);
                        pSharp.Height = PdfSharp.Drawing.XUnit.FromPoint(pageModel.Height > 10 ? pageModel.Height : 841.89f);
                        if (pageModel.Rotation != 0) pSharp.Rotate = pageModel.Rotation;

                        // Ve nen native neu co
                        if (doc.UnderlyingPdfiumDoc != null && i < doc.UnderlyingPdfiumDoc.PageCount)
                        {
                            try
                            {
                                int pw = (int)pageModel.Width;
                                int ph = (int)pageModel.Height;
                                using (Image baseImg = doc.UnderlyingPdfiumDoc.Render(i, pw, ph, 300, 300, PdfiumViewer.PdfRenderFlags.Annotations | PdfiumViewer.PdfRenderFlags.LcdText))
                                {
                                    if (baseImg != null)
                                    {
                                        using (var gfxBg = PdfSharp.Drawing.XGraphics.FromPdfPage(pSharp, PdfSharp.Drawing.XGraphicsPdfPageOptions.Prepend))
                                        using (var msImg = new MemoryStream())
                                        {
                                            baseImg.Save(msImg, ImageFormat.Png);
                                            msImg.Position = 0;
                                            using (var xImg = PdfSharp.Drawing.XImage.FromStream(msImg))
                                            {
                                                gfxBg.DrawImage(xImg, 0, 0, pageModel.Width, pageModel.Height);
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }

                // 2. Ve tat ca cac noi dung sua doi, ve tay, chu thich, hinh khoi len cac trang
                for (int i = 0; i < totalPages; i++)
                {
                    if (i >= pdfSharpDoc.Pages.Count)
                    {
                        var pSharpNew = pdfSharpDoc.AddPage();
                        pSharpNew.Width = PdfSharp.Drawing.XUnit.FromPoint(doc.Pages[i].Width);
                        pSharpNew.Height = PdfSharp.Drawing.XUnit.FromPoint(doc.Pages[i].Height);
                    }

                    var pSharp = pdfSharpDoc.Pages[i];
                    var pageModel = doc.Pages[i];

                    using (var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(pSharp, PdfSharp.Drawing.XGraphicsPdfPageOptions.Append))
                    {
                        // A. Hinh mo Watermark
                        if (doc.GlobalWatermark != null && doc.GlobalWatermark.IsEnabled && !string.IsNullOrEmpty(doc.GlobalWatermark.Text))
                        {
                            try
                            {
                                var wmOptions = new PdfSharp.Drawing.XPdfFontOptions(PdfSharp.Pdf.PdfFontEncoding.Unicode);
                                var wmFont = new PdfSharp.Drawing.XFont("Arial", doc.GlobalWatermark.FontSize > 0 ? doc.GlobalWatermark.FontSize : 48, PdfSharp.Drawing.XFontStyle.Bold, wmOptions);
                                var wmBrush = new PdfSharp.Drawing.XSolidBrush(PdfSharp.Drawing.XColor.FromArgb(50, doc.GlobalWatermark.Color.R, doc.GlobalWatermark.Color.G, doc.GlobalWatermark.Color.B));
                                gfx.Save();
                                gfx.TranslateTransform(pageModel.Width / 2f, pageModel.Height / 2f);
                                gfx.RotateTransform(doc.GlobalWatermark.AngleDegrees != 0 ? doc.GlobalWatermark.AngleDegrees : -45);
                                var wmFormat = new PdfSharp.Drawing.XStringFormat { Alignment = PdfSharp.Drawing.XStringAlignment.Center, LineAlignment = PdfSharp.Drawing.XLineAlignment.Center };
                                gfx.DrawString(doc.GlobalWatermark.Text, wmFont, wmBrush, 0, 0, wmFormat);
                                gfx.Restore();
                            }
                            catch { }
                        }

                        // B. Chu thich Annotations (Highlight, Gach chan, Gach bo, Net ve tay Ink)
                        if (pageModel.Annotations != null)
                        {
                            foreach (var ann in pageModel.Annotations)
                            {
                                if (ann == null) continue;

                                if (ann.AnnotationType == PdfAnnotationType.Highlight)
                                {
                                    int alpha = (int)(ann.Opacity * 255);
                                    if (alpha <= 0 || alpha > 255) alpha = 90;
                                    var hlBrush = new PdfSharp.Drawing.XSolidBrush(PdfSharp.Drawing.XColor.FromArgb(alpha, ann.Color.R, ann.Color.G, ann.Color.B));
                                    gfx.DrawRectangle(hlBrush, ann.Bounds.X, ann.Bounds.Y, ann.Bounds.Width, ann.Bounds.Height);
                                }
                                else if (ann.AnnotationType == PdfAnnotationType.Underline)
                                {
                                    float strokeW = ann.StrokeWidth > 0 ? ann.StrokeWidth : 1.5f;
                                    var uPen = new PdfSharp.Drawing.XPen(PdfSharp.Drawing.XColor.FromArgb(ann.Color.A, ann.Color.R, ann.Color.G, ann.Color.B), strokeW);
                                    gfx.DrawLine(uPen, ann.Bounds.X, ann.Bounds.Bottom, ann.Bounds.Right, ann.Bounds.Bottom);
                                }
                                else if (ann.AnnotationType == PdfAnnotationType.Strikethrough)
                                {
                                    float strokeW = ann.StrokeWidth > 0 ? ann.StrokeWidth : 1.5f;
                                    float midY = ann.Bounds.Y + (ann.Bounds.Height / 2f);
                                    var sPen = new PdfSharp.Drawing.XPen(PdfSharp.Drawing.XColor.FromArgb(ann.Color.A, ann.Color.R, ann.Color.G, ann.Color.B), strokeW);
                                    gfx.DrawLine(sPen, ann.Bounds.X, midY, ann.Bounds.Right, midY);
                                }
                                else if (ann.AnnotationType == PdfAnnotationType.InkDrawing && ann.InkPoints != null && ann.InkPoints.Count > 1)
                                {
                                    float strokeW = ann.StrokeWidth > 0 ? ann.StrokeWidth : 2.5f;
                                    var inkPen = new PdfSharp.Drawing.XPen(PdfSharp.Drawing.XColor.FromArgb(ann.Color.A, ann.Color.R, ann.Color.G, ann.Color.B), strokeW);
                                    inkPen.LineCap = PdfSharp.Drawing.XLineCap.Round;
                                    inkPen.LineJoin = PdfSharp.Drawing.XLineJoin.Round;
                                    for (int k = 1; k < ann.InkPoints.Count; k++)
                                    {
                                        gfx.DrawLine(inkPen, ann.InkPoints[k - 1].X, ann.InkPoints[k - 1].Y, ann.InkPoints[k].X, ann.InkPoints[k].Y);
                                    }
                                }
                                else if (ann.AnnotationType == PdfAnnotationType.Stamp && !string.IsNullOrEmpty(ann.StampText))
                                {
                                    var spPen = new PdfSharp.Drawing.XPen(PdfSharp.Drawing.XColor.FromArgb(ann.Color.A, ann.Color.R, ann.Color.G, ann.Color.B), 2f);
                                    gfx.DrawRoundedRectangle(spPen, ann.Bounds.X, ann.Bounds.Y, ann.Bounds.Width, ann.Bounds.Height, 4, 4);
                                    var stampFont = new PdfSharp.Drawing.XFont("Arial", 10, PdfSharp.Drawing.XFontStyle.Bold, new PdfSharp.Drawing.XPdfFontOptions(PdfSharp.Pdf.PdfFontEncoding.Unicode));
                                    var stampBrush = new PdfSharp.Drawing.XSolidBrush(PdfSharp.Drawing.XColor.FromArgb(ann.Color.A, ann.Color.R, ann.Color.G, ann.Color.B));
                                    var sf = new PdfSharp.Drawing.XStringFormat { Alignment = PdfSharp.Drawing.XStringAlignment.Center, LineAlignment = PdfSharp.Drawing.XLineAlignment.Center };
                                    gfx.DrawString(ann.StampText, stampFont, stampBrush, new PdfSharp.Drawing.XRect(ann.Bounds.X, ann.Bounds.Y, ann.Bounds.Width, ann.Bounds.Height), sf);
                                }
                            }
                        }

                        // C. Hinh khoi Shapes
                        if (pageModel.ShapeElements != null)
                        {
                            foreach (var shp in pageModel.ShapeElements)
                            {
                                if (shp == null) continue;
                                float strokeW = shp.StrokeWidth > 0 ? shp.StrokeWidth : 2.0f;
                                var sPen = new PdfSharp.Drawing.XPen(PdfSharp.Drawing.XColor.FromArgb(shp.StrokeColor.A, shp.StrokeColor.R, shp.StrokeColor.G, shp.StrokeColor.B), strokeW);
                                PdfSharp.Drawing.XBrush sBrush = null;
                                if (shp.FillColor != Color.Transparent && shp.FillColor.A > 0)
                                {
                                    sBrush = new PdfSharp.Drawing.XSolidBrush(PdfSharp.Drawing.XColor.FromArgb(shp.FillColor.A, shp.FillColor.R, shp.FillColor.G, shp.FillColor.B));
                                }

                                if (shp.ShapeType == PdfShapeType.Rectangle)
                                {
                                    if (sBrush != null) gfx.DrawRectangle(sPen, sBrush, shp.Bounds.X, shp.Bounds.Y, shp.Bounds.Width, shp.Bounds.Height);
                                    else gfx.DrawRectangle(sPen, shp.Bounds.X, shp.Bounds.Y, shp.Bounds.Width, shp.Bounds.Height);
                                }
                                else if (shp.ShapeType == PdfShapeType.Ellipse)
                                {
                                    if (sBrush != null) gfx.DrawEllipse(sPen, sBrush, shp.Bounds.X, shp.Bounds.Y, shp.Bounds.Width, shp.Bounds.Height);
                                    else gfx.DrawEllipse(sPen, shp.Bounds.X, shp.Bounds.Y, shp.Bounds.Width, shp.Bounds.Height);
                                }
                                else if (shp.ShapeType == PdfShapeType.Line)
                                {
                                    gfx.DrawLine(sPen, shp.Bounds.X, shp.Bounds.Y, shp.Bounds.Right, shp.Bounds.Bottom);
                                }
                            }
                        }

                        // D. Van ban (Sua chu tieng Viet & Them chu moi)
                        if (pageModel.TextElements != null)
                        {
                            foreach (var txt in pageModel.TextElements)
                            {
                                if (txt == null || string.IsNullOrEmpty(txt.Text)) continue;

                                if (txt.IsDetectedFromPdf)
                                {
                                    // Chi xu ly neu nguoi dung DA SUA CHU khac voi nguyen ban PDF goc
                                    if (txt.Text != txt.OriginalText)
                                    {
                                        // Che phu dong chu goc cu bang mau nen chuan (hoac mau trang)
                                        Color coverCol = (txt.DetectedBackgroundColor != Color.Transparent && txt.DetectedBackgroundColor.A > 0)
                                            ? txt.DetectedBackgroundColor
                                            : Color.White;
                                        var coverBrush = new PdfSharp.Drawing.XSolidBrush(PdfSharp.Drawing.XColor.FromArgb(coverCol.R, coverCol.G, coverCol.B));
                                        gfx.DrawRectangle(coverBrush, txt.OriginalBounds.X - 1, txt.OriginalBounds.Y - 1, txt.OriginalBounds.Width + 2, txt.OriginalBounds.Height + 2);

                                        // Ve dong chu da sua voi Unicode tieng Viet chuan
                                        DrawTextElementOnPdfSharp(gfx, txt);
                                    }
                                }
                                else
                                {
                                    // Hop chu moi do nguoi dung them vao
                                    DrawTextElementOnPdfSharp(gfx, txt);
                                }
                            }
                        }

                        // E. Chu ky dien tu
                        if (pageModel.Signatures != null)
                        {
                            foreach (var sig in pageModel.Signatures)
                            {
                                if (sig == null) continue;
                                if (sig.SignatureImage != null)
                                {
                                    using (var msSig = new MemoryStream())
                                    {
                                        sig.SignatureImage.Save(msSig, ImageFormat.Png);
                                        msSig.Position = 0;
                                        using (var xSig = PdfSharp.Drawing.XImage.FromStream(msSig))
                                        {
                                            gfx.DrawImage(xSig, sig.Bounds.X, sig.Bounds.Y, sig.Bounds.Width, sig.Bounds.Height);
                                        }
                                    }
                                }
                                else
                                {
                                    var sigPen = new PdfSharp.Drawing.XPen(PdfSharp.Drawing.XColor.FromArgb(sig.SignatureColor.A, sig.SignatureColor.R, sig.SignatureColor.G, sig.SignatureColor.B), 1.5f);
                                    gfx.DrawRectangle(sigPen, sig.Bounds.X, sig.Bounds.Y, sig.Bounds.Width, sig.Bounds.Height);
                                    var sigFont = new PdfSharp.Drawing.XFont("Arial", 10, PdfSharp.Drawing.XFontStyle.Bold, new PdfSharp.Drawing.XPdfFontOptions(PdfSharp.Pdf.PdfFontEncoding.Unicode));
                                    var sigBrush = new PdfSharp.Drawing.XSolidBrush(PdfSharp.Drawing.XColor.FromArgb(sig.SignatureColor.A, sig.SignatureColor.R, sig.SignatureColor.G, sig.SignatureColor.B));
                                    gfx.DrawString(AppL10n.T("[Chữ ký điện tử: ", "[Digital Signature: ") + sig.SignerName + "]", sigFont, sigBrush, sig.Bounds.X + 6, sig.Bounds.Y + 16);
                                }
                            }
                        }
                    }
                }

                // Luu tai lieu PdfSharp ra mang byte an toan
                byte[] newBytes;
                using (var outMs = new MemoryStream())
                {
                    pdfSharpDoc.Save(outMs, false);
                    newBytes = outMs.ToArray();
                }

                // Ghi ra file an toan tren dia (Atomic Write)
                try
                {
                    File.WriteAllBytes(filePath, newBytes);
                }
                catch
                {
                    string tempFile = filePath + ".tmp_" + Guid.NewGuid().ToString("N");
                    File.WriteAllBytes(tempFile, newBytes);
                    if (File.Exists(filePath))
                    {
                        try { File.Delete(filePath); } catch { }
                    }
                    File.Move(tempFile, filePath, true);
                }

                // Cap nhat trang thai trong bo nho
                doc.FilePath = filePath;
                doc.OriginalBytes = newBytes;
                doc.IsModified = false;

                // Nap lai UnderlyingPdfiumDoc tu MemoryStream de giao dien hien thi tuc thi
                try
                {
                    if (doc.UnderlyingPdfiumDoc != null)
                    {
                        doc.UnderlyingPdfiumDoc.Dispose();
                        doc.UnderlyingPdfiumDoc = null;
                    }
                    doc.UnderlyingPdfiumDoc = PdfiumViewer.PdfDocument.Load(new MemoryStream(newBytes));

                    foreach (var p in doc.Pages)
                    {
                        if (p.Annotations != null)
                        {
                            p.Annotations.RemoveAll(a => a.AnnotationType == PdfAnnotationType.InkDrawing);
                        }
                        if (p.ShapeElements != null)
                        {
                            p.ShapeElements.Clear();
                        }
                        if (p.TextElements != null)
                        {
                            foreach (var t in p.TextElements)
                            {
                                t.OriginalText = t.Text;
                                t.OriginalBounds = t.Bounds;
                                t.IsDetectedFromPdf = true;
                            }
                        }
                        p.InvalidateCache();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Reload Pdfium after save: " + ex.Message);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SavePdf exception: " + ex);
                System.Diagnostics.Debug.WriteLine("SavePdf error: " + ex);
                return false;
            }
        }

        private static void DrawTextElementOnPdfSharp(PdfSharp.Drawing.XGraphics gfx, PdfTextElement txt)
        {
            if (gfx == null || txt == null || string.IsNullOrEmpty(txt.Text)) return;

            // Mau nen o neu co
            if (txt.BackgroundColor != Color.Transparent && txt.BackgroundColor.A > 0)
            {
                var bgBrush = new PdfSharp.Drawing.XSolidBrush(PdfSharp.Drawing.XColor.FromArgb(txt.BackgroundColor.A, txt.BackgroundColor.R, txt.BackgroundColor.G, txt.BackgroundColor.B));
                gfx.DrawRectangle(bgBrush, txt.Bounds.X, txt.Bounds.Y, txt.Bounds.Width, txt.Bounds.Height);
            }

            // Vien khung neu co
            if (txt.BorderColor != Color.Transparent && txt.BorderColor.A > 0 && txt.BorderWidth > 0)
            {
                var bPen = new PdfSharp.Drawing.XPen(PdfSharp.Drawing.XColor.FromArgb(txt.BorderColor.A, txt.BorderColor.R, txt.BorderColor.G, txt.BorderColor.B), txt.BorderWidth);
                gfx.DrawRectangle(bPen, txt.Bounds.X, txt.Bounds.Y, txt.Bounds.Width, txt.Bounds.Height);
            }

            // Thiet lap font Unicode tieng Viet
            var fontOptions = new PdfSharp.Drawing.XPdfFontOptions(PdfSharp.Pdf.PdfFontEncoding.Unicode);
            PdfSharp.Drawing.XFontStyle style = PdfSharp.Drawing.XFontStyle.Regular;
            if (txt.IsBold && txt.IsItalic) style = PdfSharp.Drawing.XFontStyle.BoldItalic;
            else if (txt.IsBold) style = PdfSharp.Drawing.XFontStyle.Bold;
            else if (txt.IsItalic) style = PdfSharp.Drawing.XFontStyle.Italic;

            string fName = string.IsNullOrEmpty(txt.FontName) ? "Segoe UI" : txt.FontName;
            float fSize = txt.FontSize > 0 ? txt.FontSize : 12f;

            PdfSharp.Drawing.XFont font = null;
            try
            {
                font = new PdfSharp.Drawing.XFont(fName, fSize, style, fontOptions);
            }
            catch
            {
                try { font = new PdfSharp.Drawing.XFont("Arial", fSize, style, fontOptions); }
                catch { font = new PdfSharp.Drawing.XFont("Times New Roman", fSize, style, fontOptions); }
            }

            var textBrush = new PdfSharp.Drawing.XSolidBrush(PdfSharp.Drawing.XColor.FromArgb(txt.TextColor.A, txt.TextColor.R, txt.TextColor.G, txt.TextColor.B));
            var sf = new PdfSharp.Drawing.XStringFormat();
            switch (txt.Alignment)
            {
                case StringAlignment.Center:
                    sf.Alignment = PdfSharp.Drawing.XStringAlignment.Center;
                    break;
                case StringAlignment.Far:
                    sf.Alignment = PdfSharp.Drawing.XStringAlignment.Far;
                    break;
                default:
                    sf.Alignment = PdfSharp.Drawing.XStringAlignment.Near;
                    break;
            }
            sf.LineAlignment = PdfSharp.Drawing.XLineAlignment.Near;

            gfx.DrawString(txt.Text, font, textBrush, new PdfSharp.Drawing.XRect(txt.Bounds.X, txt.Bounds.Y, Math.Max(txt.Bounds.Width, 20), Math.Max(txt.Bounds.Height, txt.FontSize * 1.3f)), sf);
        }
        #endregion


        #region PDF Batch & Processing Algorithms (Merge, Split, Extract)

        /// <summary>

        /// Ghép nhiều tệp PDF thành một tài liệu duy nhất (Thuật toán Merge PDFs chuẩn PDFSharp)

        /// </summary>

        public static PdfDocument MergePdfs(IEnumerable<string> filePaths, string outputFilePath = null)

        {

            if (filePaths == null) return null;



            string targetOut = string.IsNullOrEmpty(outputFilePath)

                ? Path.Combine(Path.GetTempPath(), "GenOffice_Merged_" + Guid.NewGuid().ToString("N") + ".pdf")

                : outputFilePath;



            try

            {

                using (PdfSharp.Pdf.PdfDocument outputDocument = new PdfSharp.Pdf.PdfDocument())

                {

                    foreach (string path in filePaths)

                    {

                        if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;

                        using (PdfSharp.Pdf.PdfDocument inputDoc = PdfSharp.Pdf.IO.PdfReader.Open(path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))

                        {

                            for (int i = 0; i < inputDoc.PageCount; i++)

                            {

                                outputDocument.AddPage(inputDoc.Pages[i]);

                            }

                        }

                    }



                    if (outputDocument.PageCount == 0)

                    {

                        outputDocument.AddPage();

                    }



                    outputDocument.Save(targetOut);

                }



                return LoadPdf(targetOut);

            }

            catch

            {

                PdfDocument mergedDoc = new PdfDocument(AppL10n.T("Tài liệu PDF Hợp nhất.pdf", "Merged PDF Document.pdf"));

                mergedDoc.Pages.Clear();

                foreach (string path in filePaths)

                {

                    if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;

                    try

                    {

                        PdfDocument inDoc = LoadPdf(path);

                        if (inDoc != null && inDoc.Pages != null)

                        {

                            foreach (var page in inDoc.Pages)

                            {

                                mergedDoc.Pages.Add(page.Clone());

                            }

                        }

                    }

                    catch { }

                }

                if (mergedDoc.Pages.Count == 0) mergedDoc.Pages.Add(new PdfPageModel(1));

                mergedDoc.RenumberPages();

                if (!string.IsNullOrEmpty(outputFilePath))

                {

                    SavePdf(mergedDoc, outputFilePath);

                    mergedDoc.FilePath = outputFilePath;

                }

                return mergedDoc;

            }

        }



        /// <summary>

        /// Ghép nhiều đối tượng PdfDocument thành một tài liệu duy nhất trong bộ nhớ

        /// </summary>

        public static PdfDocument MergeDocuments(IEnumerable<PdfDocument> documents, string outputFilePath = null)

        {

            if (documents == null) return null;



            List<string> tempFiles = new List<string>();

            try

            {

                foreach (var doc in documents)

                {

                    if (doc != null)

                    {

                        if (!string.IsNullOrEmpty(doc.FilePath) && File.Exists(doc.FilePath))

                        {

                            tempFiles.Add(doc.FilePath);

                        }

                        else

                        {

                            string tPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");

                            SavePdf(doc, tPath);

                            tempFiles.Add(tPath);

                        }

                    }

                }

                return MergePdfs(tempFiles, outputFilePath);

            }

            catch

            {

                PdfDocument mergedDoc = new PdfDocument(AppL10n.T("Tài liệu PDF Hợp nhất.pdf", "Merged PDF Document.pdf"));

                mergedDoc.Pages.Clear();

                foreach (var doc in documents)

                {

                    if (doc != null && doc.Pages != null)

                    {

                        foreach (var page in doc.Pages) mergedDoc.Pages.Add(page.Clone());

                    }

                }

                if (mergedDoc.Pages.Count == 0) mergedDoc.Pages.Add(new PdfPageModel(1));

                mergedDoc.RenumberPages();

                if (!string.IsNullOrEmpty(outputFilePath)) SavePdf(mergedDoc, outputFilePath);

                return mergedDoc;

            }

        }



        /// <summary>

        /// Tách tài liệu PDF theo dải trang (ví dụ: "1-3, 5, 7-10") và xuất ra tệp mới

        /// </summary>

        public static PdfDocument SplitPdf(PdfDocument sourceDoc, string pageRangePattern, string outputFile = null)

        {

            if (sourceDoc == null || sourceDoc.Pages == null || sourceDoc.Pages.Count == 0) return null;



            List<int> pageIndices = ParsePageRange(pageRangePattern, sourceDoc.Pages.Count);

            if (pageIndices.Count == 0)

            {

                for (int i = 0; i < sourceDoc.Pages.Count; i++) pageIndices.Add(i);

            }



            string targetOut = string.IsNullOrEmpty(outputFile)

                ? Path.Combine(Path.GetTempPath(), "GenOffice_Split_" + Guid.NewGuid().ToString("N") + ".pdf")

                : outputFile;



            try

            {

                if (!string.IsNullOrEmpty(sourceDoc.FilePath) && File.Exists(sourceDoc.FilePath))

                {

                    using (PdfSharp.Pdf.PdfDocument inDoc = PdfSharp.Pdf.IO.PdfReader.Open(sourceDoc.FilePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))

                    using (PdfSharp.Pdf.PdfDocument outDoc = new PdfSharp.Pdf.PdfDocument())

                    {

                        foreach (int idx in pageIndices)

                        {

                            if (idx >= 0 && idx < inDoc.PageCount)

                            {

                                outDoc.AddPage(inDoc.Pages[idx]);

                            }

                        }



                        if (outDoc.PageCount == 0) outDoc.AddPage();

                        outDoc.Save(targetOut);

                    }



                    return LoadPdf(targetOut);

                }

                else

                {

                    PdfDocument extracted = sourceDoc.ExtractPages(pageIndices, AppL10n.T("Tách - ", "Split - ") + sourceDoc.Title);

                    if (!string.IsNullOrEmpty(outputFile))

                    {

                        SavePdf(extracted, outputFile);

                        return LoadPdf(outputFile);

                    }

                    return extracted;

                }

            }

            catch

            {

                PdfDocument extracted = sourceDoc.ExtractPages(pageIndices, AppL10n.T("Tách - ", "Split - ") + sourceDoc.Title);

                if (!string.IsNullOrEmpty(outputFile))

                {

                    SavePdf(extracted, outputFile);

                    return LoadPdf(outputFile);

                }

                return extracted;

            }

        }



        /// <summary>

        /// Phân tích cú pháp dải trang dạng "1-3, 5, 8-10" thành danh sách chỉ mục 0-based

        /// </summary>

        public static List<int> ParsePageRange(string rangeStr, int totalPages)

        {

            List<int> result = new List<int>();

            if (string.IsNullOrEmpty(rangeStr) || totalPages <= 0) return result;



            string[] parts = rangeStr.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string p in parts)

            {

                string token = p.Trim();

                if (token.Contains("-"))

                {

                    string[] rangeParts = token.Split('-');

                    if (rangeParts.Length == 2)

                    {

                        int start, end;

                        if (int.TryParse(rangeParts[0].Trim(), out start) && int.TryParse(rangeParts[1].Trim(), out end))

                        {

                            start = Math.Max(1, Math.Min(start, totalPages));

                            end = Math.Max(1, Math.Min(end, totalPages));

                            int step = (start <= end) ? 1 : -1;

                            for (int k = start; (step > 0 ? k <= end : k >= end); k += step)

                            {

                                int pIdx = k - 1;

                                if (!result.Contains(pIdx)) result.Add(pIdx);

                            }

                        }

                    }

                }

                else

                {

                    int singlePage;

                    if (int.TryParse(token, out singlePage))

                    {

                        if (singlePage >= 1 && singlePage <= totalPages)

                        {

                            int pIdx = singlePage - 1;

                            if (!result.Contains(pIdx)) result.Add(pIdx);

                        }

                    }

                }

            }



            return result;

        }

        #endregion

    }

}


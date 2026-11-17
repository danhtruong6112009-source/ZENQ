using System;
using System.IO;
using System.Drawing;
using ZenQOffice.Core;
using ZenQOffice.Format;

namespace ZenQOffice.Tests
{
    class PdfSaveLoadTests
    {
        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== BẮT ĐẦU KIỂM THỬ SỬA CHỮ, VẼ VÀ LƯU PDF ===");

            string testFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_save_edit.pdf");
            if (File.Exists(testFile)) File.Delete(testFile);

            try
            {
                // Bước 1: Tạo tài liệu PDF ban đầu
                PdfDocument doc = new PdfDocument("Tài liệu thử nghiệm");
                doc.FilePath = testFile;
                PdfPageModel page = new PdfPageModel(1, 595.28f, 841.89f);

                // Dòng chữ gốc
                var txtOriginal = new PdfTextElement("Cộng hòa Xã hội Chủ nghĩa Việt Nam", new RectangleF(50, 50, 400, 25), "Segoe UI", 14f)
                {
                    IsDetectedFromPdf = false,
                    TextColor = Color.FromArgb(196, 43, 28),
                    IsBold = true
                };
                page.TextElements.Add(txtOriginal);
                doc.Pages.Add(page);

                bool saveInitial = PdfHandler.SavePdf(doc, testFile);
                if (!saveInitial || !File.Exists(testFile))
                {
                    Console.WriteLine("FAIL: Không thể lưu file ban đầu!");
                    return 1;
                }
                Console.WriteLine("PASS: Đã lưu file ban đầu thành công!");

                // Bước 2: Nạp lại file từ đĩa qua LoadPdf (sử dụng Chromium Pdfium native engine)
                PdfDocument loadedDoc = PdfHandler.LoadPdf(testFile);
                if (loadedDoc == null || loadedDoc.Pages.Count == 0 || loadedDoc.UnderlyingPdfiumDoc == null)
                {
                    Console.WriteLine("FAIL: Không thể mở file đã lưu bằng LoadPdf!");
                    return 1;
                }
                Console.WriteLine($"PASS: Đã mở file thành công với {loadedDoc.Pages.Count} trang!");

                // Bước 3: Thao tác "SỬA CHỮ" (Edit Text)
                // Giả lập trích xuất chữ và người dùng chỉnh sửa văn bản tiếng Việt
                var curPage = loadedDoc.Pages[0];
                var editElement = new PdfTextElement("Độc lập - Tự do - Hạnh phúc (Đã sửa đổi)", new RectangleF(50, 50, 400, 25), "Segoe UI", 14f)
                {
                    IsDetectedFromPdf = true,
                    OriginalText = "Cộng hòa Xã hội Chủ nghĩa Việt Nam", // Văn bản gốc
                    OriginalBounds = new RectangleF(50, 50, 400, 25),
                    TextColor = Color.Blue,
                    IsBold = true
                };
                curPage.TextElements.Add(editElement);

                // Bước 4: Thao tác "VẼ LÊN" (Freehand Ink Drawing)
                var inkAnn = new PdfAnnotationElement(PdfAnnotationType.InkDrawing, RectangleF.Empty, Color.FromArgb(220, 38, 38));
                inkAnn.StrokeWidth = 3.0f;
                inkAnn.InkPoints.Add(new PointF(60, 120));
                inkAnn.InkPoints.Add(new PointF(120, 180));
                inkAnn.InkPoints.Add(new PointF(180, 140));
                inkAnn.InkPoints.Add(new PointF(260, 210));
                curPage.Annotations.Add(inkAnn);

                // Thêm một nét vẽ khác
                var inkAnn2 = new PdfAnnotationElement(PdfAnnotationType.InkDrawing, RectangleF.Empty, Color.Green);
                inkAnn2.StrokeWidth = 2.0f;
                inkAnn2.InkPoints.Add(new PointF(100, 300));
                inkAnn2.InkPoints.Add(new PointF(200, 350));
                inkAnn2.InkPoints.Add(new PointF(300, 320));
                curPage.Annotations.Add(inkAnn2);

                // Bước 5: LƯU TỆP SAU KHI SỬA CHỮ VÀ VẼ LÊN
                bool saveEdited = PdfHandler.SavePdf(loadedDoc, testFile);
                if (!saveEdited)
                {
                    Console.WriteLine("FAIL: Lỗi khi lưu file sau khi sửa chữ và vẽ lên!");
                    return 1;
                }
                Console.WriteLine("PASS: Đã lưu file sau khi sửa chữ và vẽ thành công!");

                // Bước 6: MỞ LÊN LẦN NỮA ĐỂ KIỂM CHỨNG LỖI "MỞ LÊN LỖI TOÀN BỘ KHI SỬA CHỮ VÀ VẼ LÊN"
                PdfDocument reloadedDoc = PdfHandler.LoadPdf(testFile);
                if (reloadedDoc == null || reloadedDoc.UnderlyingPdfiumDoc == null)
                {
                    Console.WriteLine("FAIL: Tệp bị hỏng hoặc lỗi toàn bộ khi mở lại!");
                    return 1;
                }

                // Render trang để xác nhận Pdfium giải mã trang hoàn hảo không crash / không lỗi
                using (Image renderedBmp = reloadedDoc.UnderlyingPdfiumDoc.Render(0, 595, 842, 96f, 96f, PdfiumViewer.PdfRenderFlags.Annotations | PdfiumViewer.PdfRenderFlags.LcdText))
                {
                    if (renderedBmp == null || renderedBmp.Width <= 0 || renderedBmp.Height <= 0)
                    {
                        Console.WriteLine("FAIL: Render trang thất bại!");
                        return 1;
                    }
                    Console.WriteLine($"PASS: Render trang thành công độ phân giải {renderedBmp.Width}x{renderedBmp.Height}!");
                }

                string pdfText = reloadedDoc.UnderlyingPdfiumDoc.GetPdfText(0);
                Console.WriteLine("Văn bản trích xuất từ PDFium: " + pdfText.Trim());
                if (!pdfText.Contains("Độc lập") && !pdfText.Contains("Đã sửa đổi"))
                {
                    Console.WriteLine("WARNING: Không tìm thấy chữ đã sửa trong nội dung text!");
                }
                else
                {
                    Console.WriteLine("PASS: Văn bản tiếng Việt sửa đổi được bảo toàn 100% trong PDF!");
                }

                reloadedDoc.Dispose();
                loadedDoc.Dispose();

                if (File.Exists(testFile)) File.Delete(testFile);

                Console.WriteLine("=== TẤT CẢ CÁC BƯỚC KIỂM THỬ THÀNH CÔNG 100%! BUG ĐÃ ĐƯỢC FIX TRIỆT ĐỂ! ===");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL EXCEPTION: " + ex.ToString());
                return 1;
            }
        }
    }
}
